using Misha.Application.Documents;
using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Api;

public static class WatchlistSmokeEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/health/watchlist", async (IWatchlistProvider provider, CancellationToken ct) =>
        {
            if (!string.Equals(provider.Name, "dev-mock", StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound();
            }

            var cases = new[]
            {
                ("P123456", WatchlistDecision.Clear, (string?)null),
                ("POTENTIAL-123", WatchlistDecision.PotentialMatch, "DEV-POTENTIAL-"),
                ("CONFIRMED-123", WatchlistDecision.ConfirmedMatch, "DEV-CONFIRMED-")
            };

            foreach (var (documentNumber, expectedDecision, expectedReferencePrefix) in cases)
            {
                ct.ThrowIfCancellationRequested();

                var passport = PassportDocument.Create(
                    Guid.NewGuid(),
                    documentNumber,
                    "ROU",
                    "SMOKE",
                    "TEST",
                    new DateOnly(1990, 1, 1),
                    "ROU",
                    new DateOnly(2030, 1, 1));

                var result = await provider.CheckAsync(passport, ct);

                if (result.Decision != expectedDecision)
                {
                    return Results.Json(
                        new { status = "unhealthy", provider = provider.Name, documentNumber, expected = expectedDecision.ToString(), actual = result.Decision.ToString() },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                if (expectedReferencePrefix is null && result.MatchReference is not null)
                {
                    return Results.Json(
                        new { status = "unhealthy", provider = provider.Name, documentNumber, error = "Clear result unexpectedly contained a match reference." },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                if (expectedReferencePrefix is not null &&
                    (result.MatchReference is null || !result.MatchReference.StartsWith(expectedReferencePrefix, StringComparison.Ordinal)))
                {
                    return Results.Json(
                        new { status = "unhealthy", provider = provider.Name, documentNumber, error = "Match result reference did not have the expected deterministic prefix." },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            }

            return Results.Ok(new
            {
                status = "healthy",
                provider = provider.Name,
                cases = new[] { "Clear", "PotentialMatch", "ConfirmedMatch" }
            });
        });

        app.MapPost("/applications/{id:guid}/watchlist/screen/parallel", async (
            Guid id,
            IPassportRepository passports,
            IWatchlistCheckRepository checks,
            IEnumerable<IWatchlistProvider> providers,
            CancellationToken ct) =>
        {
            var service = new ParallelWatchlistScreeningService(passports, checks, providers);
            var result = await service.ScreenAsync(id, ct);

            return Results.Ok(new ParallelWatchlistResponse(
                result.ApplicationId,
                result.Decision.ToString(),
                result.Providers.Select(provider => new ParallelWatchlistProviderResponse(
                    provider.Provider,
                    provider.Check.Id,
                    provider.Check.Decision.ToString(),
                    provider.Check.MatchReference,
                    provider.Check.ErrorMessage,
                    provider.Check.CheckedAtUtc)).ToArray()));
        }).RequireAuthorization(AuthorizationPolicies.DecisionWrite);
    }
}

public sealed record ParallelWatchlistResponse(
    Guid ApplicationId,
    string Decision,
    IReadOnlyList<ParallelWatchlistProviderResponse> Providers);

public sealed record ParallelWatchlistProviderResponse(
    string Provider,
    Guid CheckId,
    string Decision,
    string? MatchReference,
    string? ErrorMessage,
    DateTimeOffset? CheckedAtUtc);
