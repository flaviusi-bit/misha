using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Infrastructure.Watchlists;

public sealed class UnavailableWatchlistProvider : IWatchlistProvider
{
    public string Name => "not-configured";

    public Task<WatchlistProviderResult> CheckAsync(
        PassportDocument passport,
        CancellationToken cancellationToken) =>
        Task.FromResult(new WatchlistProviderResult(
            WatchlistDecision.Error,
            ErrorMessage: "No watchlist provider is configured."));
}
