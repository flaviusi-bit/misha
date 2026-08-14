using Misha.Application.Applications;
using Misha.Application.Messaging;
using Misha.Application.Notifications;
using Misha.Domain.Applications;
using Misha.Domain.Notifications;
using Xunit;

namespace Misha.Integration.Tests;

public sealed class ApplicationLifecycleChangedHandlerTests
{
    [Fact]
    public async Task Handler_creates_pending_notification_for_application_lifecycle_event()
    {
        var application = Application.Create("applicant-123");
        var applications = new RecordingApplicationRepository(application);
        var notifications = new RecordingNotificationRepository();
        var handler = new ApplicationLifecycleChangedHandler(applications, notifications);
        var eventId = Guid.NewGuid();
        var body = $"{{\"EventId\":\"{eventId}\",\"ApplicationId\":\"{application.Id}\",\"FromStatus\":\"Draft\",\"ToStatus\":\"Submitted\",\"Reason\":null,\"ActorReference\":\"user-1\",\"OccurredAtUtc\":\"2026-08-14T10:00:00Z\"}}";

        await handler.HandleAsync(
            new SqsMessage("message-1", "receipt-1", body, new Dictionary<string, string>()),
            CancellationToken.None);

        var notification = Assert.Single(notifications.Items);
        Assert.Equal(application.Id, notification.ApplicationId);
        Assert.Equal("applicant-123", notification.RecipientReference);
        Assert.Equal("email", notification.Channel);
        Assert.Equal("application-lifecycle-changed.v1", notification.Template);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.True(notifications.Saved);
    }

    private sealed class RecordingApplicationRepository(Application application) : IApplicationRepository
    {
        public Task<Application?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Application?>(id == application.Id ? application : null);

        public Task<Application?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<Application?>(null);

        public Task<Application> AddOrGetExistingAsync(Application value, CancellationToken cancellationToken) =>
            Task.FromResult(value);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingNotificationRepository : INotificationRepository
    {
        public List<Notification> Items { get; } = [];
        public bool Saved { get; private set; }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            Items.Add(notification);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notification>> GetPendingAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Notification>>(Items);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }
}
