namespace Misha.Domain.Etas;

public sealed class EtaAudit
{
    private EtaAudit() { }

    private EtaAudit(
        Guid id,
        Guid? etaId,
        Guid? applicationId,
        string? etaNumber,
        EtaAuditEventType eventType,
        string outcome,
        string actorReference,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        EtaId = etaId;
        ApplicationId = applicationId;
        EtaNumber = etaNumber;
        EventType = eventType;
        Outcome = outcome;
        ActorReference = actorReference;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid? EtaId { get; private set; }
    public Guid? ApplicationId { get; private set; }
    public string? EtaNumber { get; private set; }
    public EtaAuditEventType EventType { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string ActorReference { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static EtaAudit Create(
        EtaAuditEventType eventType,
        string outcome,
        string actorReference,
        Guid? etaId = null,
        Guid? applicationId = null,
        string? etaNumber = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(outcome))
            throw new ArgumentException("Audit outcome is required.", nameof(outcome));
        if (string.IsNullOrWhiteSpace(actorReference))
            throw new ArgumentException("Audit actor reference is required.", nameof(actorReference));

        return new EtaAudit(
            Guid.NewGuid(),
            etaId,
            applicationId,
            string.IsNullOrWhiteSpace(etaNumber) ? null : etaNumber.Trim(),
            eventType,
            outcome.Trim(),
            actorReference.Trim(),
            occurredAtUtc ?? DateTimeOffset.UtcNow);
    }
}

public enum EtaAuditEventType
{
    Issued,
    Verified,
    VerificationFailed,
    Revoked
}
