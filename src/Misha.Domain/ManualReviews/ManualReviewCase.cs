namespace Misha.Domain.ManualReviews;

public sealed class ManualReviewCase
{
    private ManualReviewCase() { }

    private ManualReviewCase(Guid id, Guid applicationId, string trigger, string reason)
    {
        Id = id;
        ApplicationId = applicationId;
        Trigger = trigger;
        Reason = reason;
        Status = ManualReviewStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public ManualReviewStatus Status { get; private set; }
    public string Trigger { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? AssignedToActorReference { get; private set; }
    public DateTimeOffset? AssignedAtUtc { get; private set; }
    public ManualReviewResolution? Resolution { get; private set; }
    public string? ResolutionReason { get; private set; }
    public string? ResolvedByActorReference { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public static ManualReviewCase Create(Guid applicationId, string trigger, string reason)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application id is required.", nameof(applicationId));
        if (string.IsNullOrWhiteSpace(trigger))
            throw new ArgumentException("A review trigger is required.", nameof(trigger));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A review reason is required.", nameof(reason));

        return new ManualReviewCase(Guid.NewGuid(), applicationId, trigger.Trim(), reason.Trim());
    }

    public void Assign(string actorReference)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
            throw new ArgumentException("An actor reference is required.", nameof(actorReference));

        if (Status is not (ManualReviewStatus.Pending or ManualReviewStatus.InProgress))
            throw new InvalidOperationException($"Manual review case in status '{Status}' cannot be assigned.");

        AssignedToActorReference = actorReference.Trim();
        AssignedAtUtc ??= DateTimeOffset.UtcNow;
        Status = ManualReviewStatus.InProgress;
    }

    public void Resolve(ManualReviewResolution resolution, string actorReference, string reason)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
            throw new ArgumentException("A resolving actor reference is required.", nameof(actorReference));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A resolution reason is required.", nameof(reason));
        if (Status is not (ManualReviewStatus.Pending or ManualReviewStatus.InProgress))
            throw new InvalidOperationException($"Manual review case in status '{Status}' cannot be resolved.");

        Resolution = resolution;
        ResolutionReason = reason.Trim();
        ResolvedByActorReference = actorReference.Trim();
        ResolvedAtUtc = DateTimeOffset.UtcNow;
        Status = ManualReviewStatus.Resolved;
    }
}
