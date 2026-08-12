using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Infrastructure.Watchlists;

/// <summary>
/// Deterministic watchlist provider used only by the development environment.
/// It deliberately has no network dependency or production credentials.
/// </summary>
public sealed class MockWatchlistProvider : IWatchlistProvider
{
    public string Name => "dev-mock";

    public Task<WatchlistProviderResult> CheckAsync(
        PassportDocument passport,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fingerprint = string.Join('|',
            passport.DocumentNumber,
            passport.Surname,
            passport.GivenNames);

        var decision = fingerprint.Contains("CONFIRMED", StringComparison.OrdinalIgnoreCase)
            ? new WatchlistProviderResult(
                WatchlistDecision.ConfirmedMatch,
                MatchReference: $"DEV-CONFIRMED-{Normalize(passport.DocumentNumber)}")
            : fingerprint.Contains("POTENTIAL", StringComparison.OrdinalIgnoreCase)
                ? new WatchlistProviderResult(
                    WatchlistDecision.PotentialMatch,
                    MatchReference: $"DEV-POTENTIAL-{Normalize(passport.DocumentNumber)}")
                : new WatchlistProviderResult(WatchlistDecision.Clear);

        return Task.FromResult(decision);
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Take(32).ToArray()).ToUpperInvariant();
}
