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
        }).RequireAuthorization();
    }

    private sealed record DecisionResponse(
        string Decision,
        IReadOnlyList<string> Reasons);
}
