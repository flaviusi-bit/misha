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

app.MapPost("/applications/{id:guid}/submit", async (Guid id, ApplicationService service, CancellationToken ct) =>
{
    await service.SubmitAsync(id, ct);
    return Results.NoContent();
});

app.Run();

public sealed record CreateApplicationRequest(string ApplicantReference);
