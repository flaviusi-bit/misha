using Microsoft.EntityFrameworkCore;
using Misha.Application.Documents;
using Misha.Application.Tenants;
using Misha.Domain.Applicants;
using Misha.Domain.Documents;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class ApplicantEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/applicants/{id:guid}", async (Guid id, ITenantContext tenant, MishaDbContext db, CancellationToken ct) =>
        {
            var applicant = await GetApplicant(db, tenant, id, ct);
            return applicant is null ? Results.NotFound() : Results.Ok(ToResponse(applicant));
        }).RequireAuthorization(AuthorizationPolicies.ApiRead);

        app.MapPut("/applicants/{id:guid}/profile", async (
            Guid id,
            ApplicantProfileRequest request,
            HttpContext httpContext,
            ITenantContext tenant,
            ILoggerFactory loggerFactory,
            MishaDbContext db,
            CancellationToken ct) =>
        {
            var applicant = await GetApplicant(db, tenant, id, ct);
            if (applicant is null)
                return Results.NotFound();

            try
            {
                var identity = AuditIdentityContext.From(httpContext);
                applicant.SetProfile(new ApplicantProfile(
                    request.FirstName,
                    request.LastName,
                    request.DateOfBirth,
                    request.Nationality,
                    request.CountryOfBirth,
                    request.PlaceOfBirth,
                    request.Gender,
                    request.Email,
                    request.PhoneNumber));

                await db.SaveChangesAsync(ct);
                loggerFactory.CreateLogger("Misha.Security.Audit").LogInformation(
                    "Applicant profile updated. ApplicantId={ApplicantId} ActorSubject={ActorSubject} ClientId={ClientId} ProfileCompleted={ProfileCompleted}",
                    applicant.Id,
                    identity.Subject,
                    identity.ClientId,
                    applicant.ProfileCompleted);

                return Results.Ok(ToResponse(applicant));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ApiWrite);

        app.MapPost("/applications/{id:guid}/documents/presigned-upload", async (
            Guid id,
            PresignedUploadRequest request,
            DocumentService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.CreatePreSignedUploadAsync(id, request.DocumentType, request.FileName, request.ContentType, ct);
                return Results.Ok(new PresignedUrlResponse(result.StorageKey, result.Url, DateTimeOffset.UtcNow.AddMinutes(10)));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ApiWrite);

        app.MapGet("/applications/{id:guid}/documents/{documentId:guid}/presigned-download", async (
            Guid id,
            Guid documentId,
            DocumentService service,
            CancellationToken ct) =>
        {
            try
            {
                var url = await service.CreatePreSignedDownloadAsync(id, documentId, ct);
                return Results.Ok(new PresignedUrlResponse(null, url, DateTimeOffset.UtcNow.AddMinutes(10)));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ApiRead);
    }

    private static Task<Applicant?> GetApplicant(
        MishaDbContext db,
        ITenantContext tenant,
        Guid id,
        CancellationToken ct)
    {
        if (tenant.IsAdmin)
            return db.Applicants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

        if (!tenant.TenantId.HasValue)
            return Task.FromResult<Applicant?>(null);

        return db.Applicants.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && x.TenantId == tenant.TenantId.Value,
            ct);
    }

    private static ApplicantResponse ToResponse(Applicant applicant) =>
        new(
            applicant.Id,
            applicant.ExternalReference,
            applicant.FirstName,
            applicant.LastName,
            applicant.DateOfBirth,
            applicant.Nationality,
            applicant.CountryOfBirth,
            applicant.PlaceOfBirth,
            applicant.Gender,
            applicant.Email,
            applicant.PhoneNumber,
            applicant.ProfileCompleted,
            applicant.CreatedAtUtc,
            applicant.UpdatedAtUtc);
}

public sealed record ApplicantProfileRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Nationality,
    string? CountryOfBirth,
    string? PlaceOfBirth,
    string? Gender,
    string? Email,
    string? PhoneNumber);

public sealed record ApplicantResponse(
    Guid Id,
    string ExternalReference,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? CountryOfBirth,
    string? PlaceOfBirth,
    string? Gender,
    string? Email,
    string? PhoneNumber,
    bool ProfileCompleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record PresignedUploadRequest(
    DocumentType DocumentType,
    string FileName,
    string ContentType);

public sealed record PresignedUrlResponse(
    string? StorageKey,
    Uri Url,
    DateTimeOffset ExpiresAtUtc);
