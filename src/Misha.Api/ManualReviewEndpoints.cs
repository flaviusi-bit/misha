using System.Security.Claims;
using Misha.Application.ManualReviews;
using Misha.Domain.ManualReviews;

namespace Misha.Api;

public static class ManualReviewEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/admin/manual-reviews", async (
            ManualReviewService service,
            CancellationToken ct) =>
        {
            var cases = await service.GetOpenAsync(ct);
            return Results.Ok(cases.Select(ToResponse));
        }).RequireAuthorization(AuthorizationPolicies.ReviewRead);

        app.MapGet("/admin/manual-reviews/{id:guid}", async (
            Guid id,
            ManualReviewService service,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(ToResponse(await service.GetAsync(id, ct)));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ReviewRead);

        app.MapPost("/admin/manual-reviews/{id:guid}/assign", async (
            Guid id,
            ClaimsPrincipal user,
            ManualReviewService service,
            CancellationToken ct) =>
        {
            var actor = GetActor(user);

            try
            {
                await service.AssignAsync(id, actor, ct);
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
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ReviewWrite);

        app.MapPost("/admin/manual-reviews/{id:guid}/resolve", async (
            Guid id,
            ResolveManualReviewRequest request,
            ClaimsPrincipal user,
            ManualReviewService service,
            CancellationToken ct) =>
        {
            var actor = GetActor(user);

            try
            {
                await service.ResolveAsync(id, request.Resolution, actor, request.Reason, ct);
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
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.ReviewWrite);
    }

    private static string GetActor(ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated actor identity is required.");

    private static ManualReviewResponse ToResponse(ManualReviewCase reviewCase) => new(
        reviewCase.Id,
        reviewCase.ApplicationId,
        reviewCase.Status.ToString(),
        reviewCase.Trigger,
        reviewCase.Reason,
        reviewCase.CreatedAtUtc,
        reviewCase.AssignedToActorReference,
        reviewCase.AssignedAtUtc,
        reviewCase.Resolution?.ToString(),
        reviewCase.ResolutionReason,
        reviewCase.ResolvedByActorReference,
        reviewCase.ResolvedAtUtc);

    public sealed record ResolveManualReviewRequest(
        ManualReviewResolution Resolution,
        string Reason);

    private sealed record ManualReviewResponse(
        Guid Id,
        Guid ApplicationId,
        string Status,
        string Trigger,
        string Reason,
        DateTimeOffset CreatedAtUtc,
        string? AssignedToActorReference,
        DateTimeOffset? AssignedAtUtc,
        string? Resolution,
        string? ResolutionReason,
        string? ResolvedByActorReference,
        DateTimeOffset? ResolvedAtUtc);
}
