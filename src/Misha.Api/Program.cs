using Microsoft.EntityFrameworkCore;
using Misha.Application.Applications;
using Misha.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MishaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Misha")));
builder.Services.AddScoped<IApplicationRepository, EfApplicationRepository>();
builder.Services.AddScoped<ApplicationService>();
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
