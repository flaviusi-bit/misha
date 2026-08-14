using System.Text.Json;
using Misha.Application.Applications;
using Misha.Application.Notifications;
using Misha.Domain.Notifications;

namespace Misha.Application.Messaging;

public sealed class ApplicationLifecycleChangedHandler(
    IApplicationRepository applications,
    INotificationRepository notifications) : IEventHandler
{
    public const string EventTypeName = "application.lifecycle.changed.v1";

    public string EventType => EventTypeName;

    public async Task HandleAsync(SqsMessage message, CancellationToken cancellationToken)
    {
        var lifecycleEvent = JsonSerializer.Deserialize<ApplicationLifecycleChanged>(message.Body)
            ?? throw new InvalidOperationException("Application lifecycle event payload is invalid.");

        if (lifecycleEvent.ApplicationId == Guid.Empty)
            throw new InvalidOperationException("Application lifecycle event is missing applicationId.");

        var application = await applications.GetAsync(lifecycleEvent.ApplicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application '{lifecycleEvent.ApplicationId}' was not found.");

        var payload = JsonSerializer.Serialize(new
        {
            lifecycleEvent.EventId,
            lifecycleEvent.FromStatus,
            lifecycleEvent.ToStatus,
            lifecycleEvent.Reason,
            lifecycleEvent.ActorReference,
            lifecycleEvent.OccurredAtUtc
        });

        await notifications.AddAsync(
            Notification.Create(
                application.Id,
                application.ApplicantReference,
                "email",
                "application-lifecycle-changed.v1",
                payload),
            cancellationToken);

        await notifications.SaveChangesAsync(cancellationToken);
    }
}
