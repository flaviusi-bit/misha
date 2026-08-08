using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Application.Documents;
using Misha.Application.Watchlists;
using Misha.Infrastructure.Persistence;
using Misha.Infrastructure.Watchlists;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MishaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Misha")));
builder.Services.AddScoped<IApplicationRepository, EfApplicationRepository>();
builder.Services.AddScoped<IDocumentArtifactRepository, EfDocumentArtifactRepository>();
builder.Services.AddScoped<IPassportRepository, EfPassportRepository>();
builder.Services.AddScoped<IWatchlistCheckRepository, EfWatchlistCheckRepository>();
builder.Services.AddScoped<IWatchlistProvider, UnavailableWatchlistProvider>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<PassportService>();
builder.Services.AddScoped<WatchlistService>();
builder.Services.AddHealthChecks().AddDbContextCheck<MishaDbContext>();

var app = builder.Build();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapPost("/applications", async (CreateApplicationRequest request, ApplicationService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(request.ApplicantReference, ct);
    return Results.Created($"/applications/{id}", new { id });
});

app.MapGet("/applications/{id:guid}", async (Guid id, ApplicationService service, CancellationToken ct) =>
{
    var application = await service.GetAsync(id, ct);

    return application is null
        ? Results.NotFound()
        : Results.Ok(new ApplicationResponse(
            application.Id,
            application.ApplicantReference,
            application.Status.ToString(),
            application.CreatedAtUtc,
            application.SubmittedAtUtc,
            application.ProcessingStartedAtUtc,
            application.DecidedAtUtc,
            application.CancelledAtUtc,
            application.RefusalReason));
});

app.MapPost("/applications/{id:guid}/submit", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.SubmitAsync(id, ct)));

app.MapPost("/applications/{id:guid}/process", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.StartProcessingAsync(id, ct)));

app.MapPost("/applications/{id:guid}/approve", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.ApproveAsync(id, ct)));

app.MapPost("/applications/{id:guid}/refuse", async (
    Guid id,
    RefuseApplicationRequest request,
    ApplicationService service,
    CancellationToken ct) =>
    await ExecuteCommand(() => service.RefuseAsync(id, request.Reason, ct)));

app.MapPost("/applications/{id:guid}/cancel", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.CancelAsync(id, ct)));

app.MapGet("/applications/{id:guid}/documents", async (Guid id, DocumentService service, CancellationToken ct) =>
{
    var documents = await service.GetAsync(id, ct);
    return Results.Ok(documents.Select(x => new DocumentResponse(
        x.Id,
        x.ApplicationId,
        x.DocumentType.ToString(),
        x.FileName,
        x.ContentType,
        x.SizeBytes,
        x.Sha256,
        x.StorageKey,
        x.CreatedAtUtc)));
});

app.MapPost("/applications/{id:guid}/documents", async (
    Guid id,
    DocumentRequest request,
    DocumentService service,
    CancellationToken ct) =>
{
    try
    {
        var document = await service.RegisterAsync(
            id,
            request.DocumentType,
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            request.Sha256,
            request.StorageKey,
            ct);

        return Results.Created($"/applications/{id}/documents", new DocumentResponse(
            document.Id,
            document.ApplicationId,
            document.DocumentType.ToString(),
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.Sha256,
            document.StorageKey,
            document.CreatedAtUtc));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/applications/{id:guid}/passport", async (
    Guid id,
    PassportRequest request,
    PassportService service,
    CancellationToken ct) =>
{
    await service.CreateAsync(
        id,
        request.DocumentNumber,
        request.IssuingCountry,
        request.Surname,
        request.GivenNames,
        request.DateOfBirth,
        request.Nationality,
        request.ExpiryDate,
        ct);

    return Results.NoContent();
});

app.MapGet("/applications/{id:guid}/passport", async (Guid id, PassportService service, CancellationToken ct) =>
{
    var passport = await service.GetAsync(id, ct);
    return passport is null
        ? Results.NotFound()
        : Results.Ok(new PassportResponse(
            passport.Id,
            passport.ApplicationId,
            passport.DocumentNumber,
            passport.IssuingCountry,
            passport.Surname,
            passport.GivenNames,
            passport.DateOfBirth,
            passport.Nationality,
            passport.ExpiryDate,
            passport.IsExpired(DateOnly.FromDateTime(DateTime.UtcNow))));
});

app.MapPost("/applications/{id:guid}/watchlist/screen", async (
    Guid id,
    WatchlistService service,
    CancellationToken ct) =>
{
    var check = await service.ScreenAsync(id, ct);
    return Results.Ok(new WatchlistResponse(
        check.Id,
        check.ApplicationId,
        check.Provider,
        check.Decision.ToString(),
        check.MatchReference,
        check.ErrorMessage,
        check.CheckedAtUtc));
});

app.MapGet("/applications/{id:guid}/watchlist", async (Guid id, WatchlistService service, CancellationToken ct) =>
{
    var check = await service.GetLatestAsync(id, ct);
    return check is null
        ? Results.NotFound()
        : Results.Ok(new WatchlistResponse(
            check.Id,
            check.ApplicationId,
            check.Provider,
            check.Decision.ToString(),
            check.MatchReference,
            check.ErrorMessage,
            check.CheckedAtUtc));
});

app.Run();

static async Task<IResult> ExecuteCommand(Func<Task> command)
{
    try
    {
        await command();
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}

public sealed record CreateApplicationRequest(string ApplicantReference);
public sealed record RefuseApplicationRequest(string Reason);

public sealed record ApplicationResponse(
    Guid Id,
    string ApplicantReference,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ProcessingStartedAtUtc,
    DateTimeOffset? DecidedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? RefusalReason);

public sealed record DocumentRequest(
    Misha.Domain.Documents.DocumentType DocumentType,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string StorageKey);

public sealed record DocumentResponse(
    Guid Id,
    Guid ApplicationId,
    string DocumentType,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string StorageKey,
    DateTimeOffset CreatedAtUtc);

public sealed record PassportRequest(
    string DocumentNumber,
    string IssuingCountry,
    string Surname,
    string GivenNames,
    DateOnly DateOfBirth,
    string Nationality,
    DateOnly ExpiryDate);

public sealed record PassportResponse(
    Guid Id,
    Guid ApplicationId,
    string DocumentNumber,
    string IssuingCountry,
    string Surname,
    string GivenNames,
    DateOnly DateOfBirth,
    string Nationality,
    DateOnly ExpiryDate,
    bool IsExpired);

public sealed record WatchlistResponse(
    Guid Id,
    Guid ApplicationId,
    string Provider,
    string Decision,
    string? MatchReference,
    string? ErrorMessage,
    DateTimeOffset? CheckedAtUtc);
