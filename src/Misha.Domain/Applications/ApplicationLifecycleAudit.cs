namespace Misha.Domain.Applications;

public sealed class ApplicationLifecycleAudit
{
    private ApplicationLifecycleAudit() { }

    private ApplicationLifecycleAudit(Guid id, Guid applicationId, ApplicationStatus? fromStatus, ApplicationStatus toStatus, string? reason, string actorReference)
    {
        Id = id;
        ApplicationId = applicationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
        ActorReference = actorReference;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public ApplicationStatus? FromStatus { get; private set; }
    public ApplicationStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public string ActorReference { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static ApplicationLifecycleAudit Create(Guid applicationId, ApplicationStatus? fromStatus, ApplicationStatus toStatus, string actorReference, string? reason = null)
    {
        if (applicationId == Guid.Empty) throw new ArgumentException("Application id is required.", nameof(applicationId));
        if (string.IsNullOrWhiteSpace(actorReference)) throw new ArgumentException("Actor reference is required.", nameof(actorReference));
        if (actorReference.Trim().Length > 200) throw new ArgumentException("Actor reference must be 200 characters or fewer.", nameof(actorReference));
        if (reason is not null && reason.Length > 1000) throw new ArgumentException("Lifecycle audit reason must be 1000 characters or fewer.", nameof(reason));

        return new ApplicationLifecycleAudit(Guid.NewGuid(), applicationId, fromStatus, toStatus,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), actorReference.Trim());
    }
}
