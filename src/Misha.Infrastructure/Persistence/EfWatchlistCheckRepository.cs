using Microsoft.EntityFrameworkCore;
using Misha.Application.Watchlists;
using Misha.Domain.Watchlists;

namespace Misha.Infrastructure.Persistence;

public sealed class EfWatchlistCheckRepository(MishaDbContext db) : IWatchlistCheckRepository
{
    public Task<WatchlistCheck?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
        db.WatchlistChecks
            .Where(x => x.ApplicationId == applicationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddAsync(WatchlistCheck check, CancellationToken cancellationToken)
    {
        db.WatchlistChecks.Add(check);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
