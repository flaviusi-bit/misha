using Microsoft.EntityFrameworkCore;
using Misha.Application.Notifications;
using Misha.Domain.Notifications;
using Misha.Infrastructure.Persistence;

namespace Misha.Infrastructure.Notifications;

public sealed class EfNotificationRepository(MishaDbContext db) : INotificationRepository
{
    public Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        db.Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Notification>> GetPendingAsync(int limit, CancellationToken cancellationToken) =>
        await db.Notifications
            .Where(x => x.Status == NotificationStatus.Pending || x.Status == NotificationStatus.Failed)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
