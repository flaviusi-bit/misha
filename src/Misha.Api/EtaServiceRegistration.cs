using Misha.Application.Etas;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class EtaServiceRegistration
{
    public static void AddEtaServices(IServiceCollection services)
    {
        services.AddScoped<IEtaRepository, EfEtaRepository>();
        services.AddScoped<EtaService>();
    }

    public static void MapEtaEndpoints(this WebApplication app)
    {
        app.MapPost("/applications/{id:guid}/eta", async (
            Guid id,
            EtaService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.IssueAsync(id, ct);
                var response = ToResponse(result);
                return result.Created
                    ? Results.Created($"/applications/{id}/eta", response)
                    : Results.Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        app.MapGet("/applications/{id:guid}/eta", async (
            Guid id,
            EtaService service,
            CancellationToken ct) =>
        {
            var eta = await service.GetAsync(id, ct);
            return eta is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(new EtaIssueResult(eta, null, false)));
        }).RequireAuthorization();
    }

    private static EtaResponse ToResponse(EtaIssueResult result) => new(
        result.Eta.Id,
        result.Eta.ApplicationId,
        result.Eta.EtaNumber,
        result.Eta.Status.ToString(),
        result.Eta.IssuedAtUtc,
        result.Eta.ExpiresAtUtc,
        result.Eta.RevokedAtUtc,
        result.Eta.RevocationReason,
        result.VerificationToken);
}

public sealed record EtaResponse(
    Guid Id,
    Guid ApplicationId,
    string EtaNumber,
    string Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string? RevocationReason,
    string? VerificationToken);
