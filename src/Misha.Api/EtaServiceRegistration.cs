using System.Security.Claims;
using Misha.Application.Etas;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class EtaServiceRegistration
{
    public static void AddEtaServices(IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<IEtaRepository, EfEtaRepository>();
        services.AddScoped<IEtaAuditRepository, EfEtaAuditRepository>();
        var validityDays = configuration?.GetValue<int?>("Eta:ValidityDays") ?? 90;
        services.AddScoped<EtaService>(sp => new EtaService(
            sp.GetRequiredService<Misha.Application.Applications.IApplicationRepository>(),
            sp.GetRequiredService<Misha.Application.Payments.IPaymentRepository>(),
            sp.GetRequiredService<IEtaRepository>(),
            sp.GetRequiredService<IEtaAuditRepository>(),
            validityDays));
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

        app.MapPost("/applications/{id:guid}/eta/revoke", async (
            Guid id,
            EtaRevocationRequest request,
            ClaimsPrincipal user,
            EtaService service,
            CancellationToken ct) =>
        {
            try
            {
                var actorReference = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub")
                    ?? "authenticated-user";

                await service.RevokeAsync(id, request.Reason, actorReference, ct);
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
        }).RequireAuthorization();

        // Public by design: only the opaque ETA number + secret verification token are accepted.
        // No applicant or application data is returned by this endpoint.
        app.MapPost("/eta/verify", async (
            EtaVerificationRequest request,
            EtaService service,
            CancellationToken ct) =>
        {
            var result = await service.VerifyAsync(request.EtaNumber, request.VerificationToken, ct);
            return result is null
                ? Results.NotFound()
                : Results.Ok(new EtaVerificationResponse(
                    result.EtaNumber,
                    result.Status.ToString(),
                    result.IssuedAtUtc,
                    result.ExpiresAtUtc,
                    result.RevokedAtUtc));
        });
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

public sealed record EtaRevocationRequest(string Reason);

public sealed record EtaVerificationRequest(string EtaNumber, string VerificationToken);

public sealed record EtaVerificationResponse(
    string EtaNumber,
    string Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc);
