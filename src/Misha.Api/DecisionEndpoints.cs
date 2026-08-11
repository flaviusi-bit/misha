using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Misha.Application.Decisions;

namespace Misha.Api;

public static class DecisionEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/applications/{id:guid}/decision", async (
            Guid id,
            ClaimsPrincipal user,
            DecisionService service,
            CancellationToken ct) =>
        {
            var actor = user.FindFirstValue("sub")
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "authenticated-user";

            try
            {
                var result = await service.DecideAsync(id, actor, ct);
                return Results.Ok(new DecisionResponse(
                    result.Decision.ToString(),
                    result.Reasons));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { error = "Application changed while the decision was being applied. Re-evaluate before deciding again." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization(AuthorizationPolicies.DecisionWrite);

        app.MapGet("/applications/{id:guid}/decision/audit", async (
            Guid id,
            IDecisionAuditRepository audits,
            CancellationToken ct) =>
        {
            var records = await audits.GetByApplicationAsync(id, ct);
            return Results.Ok(records.Select(ToAuditResponse));
        }).RequireAuthorization(AuthorizationPolicies.DecisionRead);
    }

    private static DecisionAuditResponse ToAuditResponse(Misha.Domain.Decisions.DecisionAudit audit) => new(
        audit.Id,
        audit.ApplicationId,
        audit.PolicyVersion,
        audit.PolicyDecision,
        audit.Decision,
        audit.ReasonsJson,
        audit.ActorReference,
        audit.CreatedAtUtc);

    private sealed record DecisionResponse(
        string Decision,
        IReadOnlyList<string> Reasons);

    private sealed record DecisionAuditResponse(
        Guid Id,
        Guid ApplicationId,
        string PolicyVersion,
        string PolicyDecision,
        string Decision,
        string ReasonsJson,
        string ActorReference,
        DateTimeOffset CreatedAtUtc);
}
