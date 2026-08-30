using Misha.Domain.Notifications;

namespace Misha.Application.Notifications;

public interface INotificationDelivery
{
    Task DeliverAsync(Notification notification, CancellationToken cancellationToken);
}
