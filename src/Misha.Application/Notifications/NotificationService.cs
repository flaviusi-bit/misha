using Misha.Domain.Notifications;

namespace Misha.Application.Notifications;

public sealed class NotificationService(INotificationRepository repository)
{
    public async Task<Guid> QueueAsync(
        Guid applicationId,
        string recipientReference,
        string channel,
        string template,
        string payload,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(applicationId, recipientReference, channel, template, payload);
        await repository.AddAsync(notification, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return notification.Id;
    }

    public Task<IReadOnlyList<Notification>> GetPendingAsync(int limit, CancellationToken cancellationToken) =>
        repository.GetPendingAsync(Math.Clamp(limit, 1, 100), cancellationToken);
}
