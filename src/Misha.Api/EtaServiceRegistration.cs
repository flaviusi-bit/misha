using System.Security.Claims;
using System.Text;
using Misha.Application.Etas;
using Misha.Infrastructure;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class EtaServiceRegistration
{
    public static void AddEtaServices(IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<IEtaRepository, EfEtaRepository>();
        services.AddScoped<IEtaAuditRepository, EfEtaAuditRepository>();

        var signingKeyId = configuration?["Eta:Signing:KeyId"];
        var signingPrivateKeyPem = configuration?["Eta:Signing:PrivateKeyPem"];
        if (!string.IsNullOrWhiteSpace(signingKeyId) && !string.IsNullOrWhiteSpace(signingPrivateKeyPem))
        {
            services.AddSingleton<IEtaCredentialSigner>(_ =>
                new EcdsaEtaCredentialSigner(signingKeyId, signingPrivateKeyPem));
        }
        else
        {
            services.AddSingleton<IEtaCredentialSigner, DisabledEtaCredentialSigner>();
        }

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
        app.MapPost("/applications/{id:guid}/eta", async (Guid id, EtaService service, IEtaCredentialSigner signer, CancellationToken ct) =>
        {
            try
            {
                var result = await service.IssueAsync(id, ct);
                var response = ToResponse(result, app.Configuration, signer);
                return result.Created
                    ? Results.Created($"/applications/{id}/eta", response)
                    : Results.Ok(response);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization();

        app.MapGet("/applications/{id:guid}/eta", async (Guid id, EtaService service, IEtaCredentialSigner signer, CancellationToken ct) =>
        {
            var eta = await service.GetAsync(id, ct);
            return eta is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(new EtaIssueResult(eta, null, false), app.Configuration, signer));
        }).RequireAuthorization();

        app.MapPost("/applications/{id:guid}/eta/revoke", async (Guid id, EtaRevocationRequest request, ClaimsPrincipal user, EtaService service, CancellationToken ct) =>
        {
            try
            {
                var actorReference = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? "authenticated-user";
                await service.RevokeAsync(id, request.Reason, actorReference, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        }).RequireAuthorization();

        // Public by design: the token is carried only in the URL fragment and is never sent
        // to the server as part of the initial GET request. The page submits it to the rate-limited
        // verification API after loading.
        app.MapGet("/eta/verify/{etaNumber}", (HttpResponse response) =>
        {
            var nonce = EtaVerificationPage.CreateNonce();
            EtaVerificationPage.ApplySecurityHeaders(response, nonce);
            return Results.Content(EtaVerificationPage.Create(nonce), "text/html", Encoding.UTF8);
        });

        app.MapGet("/eta/verification-keys/{keyId}", (string keyId, IEtaCredentialSigner signer) =>
        {
            if (!signer.IsEnabled || !string.Equals(signer.KeyId, keyId, StringComparison.Ordinal))
                return Results.NotFound();

            return Results.Ok(new EtaVerificationKeyResponse(
                signer.KeyId,
                signer.Algorithm,
                signer.PublicKeyPem!));
        });

        // Public by design: only the opaque ETA number + secret verification token are accepted.
        // No applicant or application data is returned by this endpoint.
        app.MapPost("/eta/verify", async (EtaVerificationRequest request, EtaService service, IEtaCredentialSigner signer, CancellationToken ct) =>
        {
            var result = await service.VerifyAsync(request.EtaNumber, request.VerificationToken, ct);
            return result is null
                ? Results.NotFound()
                : Results.Ok(new EtaVerificationResponse(
                    result.EtaNumber,
                    result.Status.ToString(),
                    result.IssuedAtUtc,
                    result.ExpiresAtUtc,
                    result.RevokedAtUtc,
                    signer.Sign(result.EtaNumber, result.IssuedAtUtc, result.ExpiresAtUtc),
                    signer.IsEnabled ? signer.KeyId : null,
                    signer.IsEnabled ? signer.Algorithm : null));
        }).RequireRateLimiting("eta-verification");
    }

    private static EtaResponse ToResponse(
        EtaIssueResult result,
        IConfiguration configuration,
        IEtaCredentialSigner signer)
    {
        var verificationUrl = EtaVerificationUrl.Create(
            configuration["Eta:PublicBaseUrl"],
            result.Eta.EtaNumber,
            result.VerificationToken ?? string.Empty);

        return new EtaResponse(
            result.Eta.Id,
            result.Eta.ApplicationId,
            result.Eta.EtaNumber,
            result.Eta.Status.ToString(),
            result.Eta.IssuedAtUtc,
            result.Eta.ExpiresAtUtc,
            result.Eta.RevokedAtUtc,
            result.Eta.RevocationReason,
            result.VerificationToken,
            verificationUrl,
            signer.Sign(result.Eta.EtaNumber, result.Eta.IssuedAtUtc, result.Eta.ExpiresAtUtc),
            signer.IsEnabled ? signer.KeyId : null,
            signer.IsEnabled ? signer.Algorithm : null);
    }
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
    string? VerificationToken,
    string? VerificationUrl,
    string? Signature,
    string? SigningKeyId,
    string? SigningAlgorithm);

public sealed record EtaRevocationRequest(string Reason);
public sealed record EtaVerificationRequest(string EtaNumber, string VerificationToken);
public sealed record EtaVerificationResponse(
    string EtaNumber,
    string Status,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string? Signature,
    string? SigningKeyId,
    string? SigningAlgorithm);
public sealed record EtaVerificationKeyResponse(string KeyId, string Algorithm, string PublicKeyPem);
