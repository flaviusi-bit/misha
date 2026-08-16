namespace Misha.Domain.Applications;

public sealed class Application
{
    private Application() { }

    private Application(Guid id, Guid applicantId, string applicantReference, string? idempotencyKey)
    {
        Id = id;
        ApplicantId = applicantId;
        ApplicantReference = applicantReference;
        IdempotencyKey = idempotencyKey;
        Status = ApplicationStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicantId { get; private set; }
    // Retained as an immutable request snapshot for compatibility and audit readability.
    public string ApplicantReference { get; private set; } = string.Empty;
    public string? IdempotencyKey { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? RefusalReason { get; private set; }

    // PostgreSQL maps this property to the implicit xmin column for optimistic concurrency.
    public uint Version { get; private set; }

    public static Application Create(Guid applicantId, string applicantReference, string? idempotencyKey = null)
    {
        if (applicantId == Guid.Empty)
            throw new ArgumentException("Applicant id is required.", nameof(applicantId));

        if (string.IsNullOrWhiteSpace(applicantReference))
            throw new ArgumentException("Applicant reference is required.", nameof(applicantReference));

        var normalizedKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (normalizedKey is not null && normalizedKey.Length > 200)
            throw new ArgumentException("Idempotency key must be 200 characters or fewer.", nameof(idempotencyKey));

        return new Application(Guid.NewGuid(), applicantId, applicantReference.Trim(), normalizedKey);
    }

    // Test/compatibility factory for callers that do not yet have persistence-backed applicant identity.
    public static Application Create(string applicantReference, string? idempotencyKey = null) =>
        Create(Guid.NewGuid(), applicantReference, idempotencyKey);

    public void Submit()
    {
        EnsureStatus(ApplicationStatus.Draft);
        Status = ApplicationStatus.Submitted;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
    }

    public void StartProcessing()
    {
        EnsureStatus(ApplicationStatus.Submitted);
        Status = ApplicationStatus.Processing;
        ProcessingStartedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Approve()
    {
        EnsureStatus(ApplicationStatus.Processing);
        Status = ApplicationStatus.Approved;
        DecidedAtUtc = DateTimeOffset.UtcNow;
        RefusalReason = null;
    }

    public void Refuse(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A refusal reason is required.", nameof(reason));

        EnsureStatus(ApplicationStatus.Processing);
        Status = ApplicationStatus.Refused;
        DecidedAtUtc = DateTimeOffset.UtcNow;
        RefusalReason = reason.Trim();
    }

    public void Cancel()
    {
        if (Status is not (ApplicationStatus.Draft or ApplicationStatus.Submitted or ApplicationStatus.Processing))
            throw new InvalidOperationException($"Applications in status '{Status}' cannot be cancelled.");

        Status = ApplicationStatus.Cancelled;
        CancelledAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureStatus(ApplicationStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Application in status '{Status}' cannot transition to the requested state.");
    }
}
