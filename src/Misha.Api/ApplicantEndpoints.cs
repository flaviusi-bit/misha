using Microsoft.EntityFrameworkCore;
using Misha.Domain.Applicants;
using Misha.Infrastructure.Persistence;

namespace Misha.Api;

public static class ApplicantEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/applicants/{id:guid}", async (Guid id, MishaDbContext db, CancellationToken ct) =>
        {
            var applicant = await db.Applicants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            return applicant is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(applicant));
        }).RequireAuthorization(AuthorizationPolicies.ApiRead);

        app.MapPut("/applicants/{id:guid}/profile", async (
            Guid id,
            ApplicantProfileRequest request,
            MishaDbContext db,
            CancellationToken ct) =>
        {
            var applicant = await db.Applicants.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (applicant is null)
                return Results.NotFound();

            try
            {
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
                return Results.Ok(ToResponse(applicant));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ApiWrite);
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
