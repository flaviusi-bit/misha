using Misha.Domain.Watchlists;

namespace Misha.Application.Watchlists;

public interface IWatchlistCheckRepository
{
    Task<WatchlistCheck?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken);
    Task AddAsync(WatchlistCheck check, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
