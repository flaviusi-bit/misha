using Misha.Domain.Notifications;

namespace Misha.Application.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> GetPendingAsync(int limit, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
