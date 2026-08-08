using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Misha.Api;
using Misha.Application.Applications;
using Misha.Application.Documents;
using Misha.Application.Policy;
using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Infrastructure.Documents;
using Misha.Infrastructure.Persistence;
using Misha.Infrastructure.Storage;
using Misha.Infrastructure.Watchlists;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MishaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Misha")));
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
builder.Services.AddScoped<IDocumentStorage>(sp =>
    new S3DocumentStorage(
        sp.GetRequiredService<IAmazonS3>(),
        builder.Configuration["DocumentStorage:BucketName"] ?? string.Empty));
builder.Services.AddScoped<IApplicationRepository, EfApplicationRepository>();
builder.Services.AddScoped<IDocumentArtifactRepository, EfDocumentArtifactRepository>();
builder.Services.AddScoped<IPassportRepository, EfPassportRepository>();
builder.Services.AddScoped<IWatchlistCheckRepository, EfWatchlistCheckRepository>();
builder.Services.AddScoped<IWatchlistProvider, UnavailableWatchlistProvider>();
builder.Services.AddScoped<IPassportVerificationProvider, HttpPassportVerificationProvider>();
builder.Services.AddHttpClient("passport-verification", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<PassportService>();
builder.Services.AddScoped<PassportVerificationService>();
builder.Services.AddScoped<WatchlistService>();
builder.Services.AddSingleton<IPolicyEngine, DefaultPolicyEngine>();
builder.Services.AddScoped<PolicyService>();
Misha.Api.DecisionServiceRegistration.AddDecisionEngine(builder.Services);
PaymentServiceRegistration.AddPaymentServices(builder.Services);
builder.Services.AddHealthChecks().AddDbContextCheck<MishaDbContext>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapPaymentEndpoints();

app.MapPost("/applications", async (CreateApplicationRequest request, ApplicationService service, CancellationToken ct) =>
{
    var id = await service.CreateAsync(request.ApplicantReference, ct);
    return Results.Created($"/applications/{id}", new { id });
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapPost("/applications/{id:guid}/submit", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.SubmitAsync(id, ct))).RequireAuthorization();

app.MapPost("/applications/{id:guid}/process", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.StartProcessingAsync(id, ct))).RequireAuthorization();

app.MapPost("/applications/{id:guid}/approve", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.ApproveAsync(id, ct))).RequireAuthorization();

app.MapPost("/applications/{id:guid}/refuse", async (
    Guid id,
    RefuseApplicationRequest request,
    ApplicationService service,
    CancellationToken ct) =>
    await ExecuteCommand(() => service.RefuseAsync(id, request.Reason, ct))).RequireAuthorization();

app.MapPost("/applications/{id:guid}/cancel", async (Guid id, ApplicationService service, CancellationToken ct) =>
    await ExecuteCommand(() => service.CancelAsync(id, ct))).RequireAuthorization();

app.MapGet("/applications/{id:guid}/documents", async (Guid id, DocumentService service, CancellationToken ct) =>
{
    var documents = await service.GetAsync(id, ct);
    return Results.Ok(documents.Select(ToDocumentResponse));
}).RequireAuthorization();

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

        return Results.Created($"/applications/{id}/documents", ToDocumentResponse(document));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/applications/{id:guid}/documents/upload", async (
    Guid id,
    [FromForm] DocumentType documentType,
    [FromForm] IFormFile file,
    DocumentService service,
    CancellationToken ct) =>
{
    try
    {
        if (file is null)
            return Results.BadRequest(new { error = "A document file is required." });

        var document = await service.UploadAsync(
            id,
            documentType,
            file.FileName,
            file.ContentType,
            file.OpenReadStream(),
            ct);

        return Results.Created($"/applications/{id}/documents", ToDocumentResponse(document));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).DisableAntiforgery().RequireAuthorization();

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
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapPost("/applications/{id:guid}/passport/verify", async (
    Guid id,
    PassportVerificationService service,
    CancellationToken ct) =>
{
    try
    {
        var result = await service.VerifyAsync(id, ct);
        return Results.Ok(new PassportVerificationResponse(
            result.Decision.ToString(),
            result.Reference,
            result.ErrorMessage));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
}).RequireAuthorization();

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
}).RequireAuthorization();

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
}).RequireAuthorization();

app.MapPost("/applications/{id:guid}/policy/evaluate", async (
    Guid id,
    PolicyService service,
    CancellationToken ct) =>
{
    try
    {
        var evaluation = await service.EvaluateAsync(id, ct);
        return Results.Ok(new PolicyEvaluationResponse(
            evaluation.Decision.ToString(),
            evaluation.Reasons));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
}).RequireAuthorization();

app.Run();

static DocumentResponse ToDocumentResponse(DocumentArtifact document) => new(
    document.Id,
    document.ApplicationId,
    document.DocumentType.ToString(),
    document.FileName,
    document.ContentType,
    document.SizeBytes,
    document.Sha256,
    document.StorageKey,
    document.CreatedAtUtc);

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
    DocumentType DocumentType,
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

public sealed record PassportVerificationResponse(
    string Decision,
    string? Reference,
    string? ErrorMessage);

public sealed record WatchlistResponse(
    Guid Id,
    Guid ApplicationId,
    string Provider,
    string Decision,
    string? MatchReference,
    string? ErrorMessage,
    DateTimeOffset? CheckedAtUtc);

public sealed record PolicyEvaluationResponse(
    string Decision,
    IReadOnlyList<string> Reasons);
