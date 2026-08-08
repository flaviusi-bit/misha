namespace Misha.Domain.Decisions;

public sealed class DecisionAudit
{
    private DecisionAudit() { }

    private DecisionAudit(
        Guid id,
        Guid applicationId,
        string policyVersion,
        string policyDecision,
        string decision,
        string reasonsJson,
        string actorReference)
    {
        Id = id;
        ApplicationId = applicationId;
        PolicyVersion = policyVersion;
        PolicyDecision = policyDecision;
        Decision = decision;
        ReasonsJson = reasonsJson;
        ActorReference = actorReference;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string PolicyVersion { get; private set; } = string.Empty;
    public string PolicyDecision { get; private set; } = string.Empty;
    public string Decision { get; private set; } = string.Empty;
    public string ReasonsJson { get; private set; } = "[]";
    public string ActorReference { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static DecisionAudit Create(
        Guid applicationId,
        string policyVersion,
        string policyDecision,
        string decision,
        string reasonsJson,
        string actorReference)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application id is required.", nameof(applicationId));
        if (string.IsNullOrWhiteSpace(policyVersion))
            throw new ArgumentException("Policy version is required.", nameof(policyVersion));
        if (string.IsNullOrWhiteSpace(policyDecision))
            throw new ArgumentException("Policy decision is required.", nameof(policyDecision));
        if (string.IsNullOrWhiteSpace(decision))
            throw new ArgumentException("Decision is required.", nameof(decision));
        if (string.IsNullOrWhiteSpace(actorReference))
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));

        return new DecisionAudit(
            Guid.NewGuid(),
            applicationId,
            policyVersion.Trim(),
            policyDecision.Trim(),
            decision.Trim(),
            reasonsJson,
            actorReference.Trim());
    }
}
