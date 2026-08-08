using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Application.Watchlists;

public interface IWatchlistProvider
{
    string Name { get; }

    Task<WatchlistProviderResult> CheckAsync(
        PassportDocument passport,
        CancellationToken cancellationToken);
}

public sealed record WatchlistProviderResult(
    WatchlistDecision Decision,
    string? MatchReference = null,
    string? ErrorMessage = null);
