namespace Misha.Domain.Notifications;

public sealed class Notification
{
    private Notification() { }

    private Notification(Guid id, Guid applicationId, string recipientReference, string channel, string template, string payload)
    {
        Id = id;
        ApplicationId = applicationId;
        RecipientReference = recipientReference;
        Channel = channel;
        Template = template;
        Payload = payload;
        Status = NotificationStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string RecipientReference { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public string Template { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }

    public static Notification Create(Guid applicationId, string recipientReference, string channel, string template, string payload)
    {
        if (applicationId == Guid.Empty) throw new ArgumentException("Application id is required.", nameof(applicationId));
        if (string.IsNullOrWhiteSpace(recipientReference)) throw new ArgumentException("Recipient reference is required.", nameof(recipientReference));
        if (string.IsNullOrWhiteSpace(channel)) throw new ArgumentException("Channel is required.", nameof(channel));
        if (string.IsNullOrWhiteSpace(template)) throw new ArgumentException("Template is required.", nameof(template));
        return new Notification(Guid.NewGuid(), applicationId, recipientReference.Trim(), channel.Trim(), template.Trim(), payload ?? string.Empty);
    }

    public void MarkSent()
    {
        if (Status == NotificationStatus.Sent) return;
        Status = NotificationStatus.Sent;
        SentAtUtc = DateTimeOffset.UtcNow;
        LastAttemptAtUtc = SentAtUtc;
        Attempts++;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        if (Status == NotificationStatus.Sent) throw new InvalidOperationException("A sent notification cannot fail.");
        Status = NotificationStatus.Failed;
        LastAttemptAtUtc = DateTimeOffset.UtcNow;
        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Notification delivery failed." : error.Trim();
    }
}
