using Misha.Domain.Notifications;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class NotificationTests
{
    [Fact]
    public void Create_starts_pending()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            "traveller-001",
            "email",
            "application-submitted",
            "{\"applicationId\":\"123\"}");

        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(0, notification.Attempts);
    }

    [Fact]
    public void MarkSent_records_delivery_attempt()
    {
        var notification = CreateNotification();

        notification.MarkSent();

        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, notification.Attempts);
        Assert.NotNull(notification.SentAtUtc);
        Assert.NotNull(notification.LastAttemptAtUtc);
        Assert.Null(notification.LastError);
    }

    [Fact]
    public void MarkFailed_records_error_and_attempt()
    {
        var notification = CreateNotification();

        notification.MarkFailed("provider timeout");

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal(1, notification.Attempts);
        Assert.Equal("provider timeout", notification.LastError);
        Assert.NotNull(notification.LastAttemptAtUtc);
    }

    [Fact]
    public void Sent_notification_cannot_be_marked_failed()
    {
        var notification = CreateNotification();
        notification.MarkSent();

        Assert.Throws<InvalidOperationException>(() => notification.MarkFailed("late failure"));
    }

    private static Notification CreateNotification() => Notification.Create(
        Guid.NewGuid(),
        "traveller-001",
        "email",
        "application-submitted",
        "{}");
}
