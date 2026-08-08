namespace Misha.Domain.Applications;

public sealed class Application
{
    private Application() { }

    private Application(Guid id, string applicantReference)
    {
        Id = id;
        ApplicantReference = applicantReference;
        Status = ApplicationStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string ApplicantReference { get; private set; } = string.Empty;
    public ApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public static Application Create(string applicantReference)
    {
        if (string.IsNullOrWhiteSpace(applicantReference))
            throw new ArgumentException("Applicant reference is required.", nameof(applicantReference));

        return new Application(Guid.NewGuid(), applicantReference.Trim());
    }

    public void Submit()
    {
        if (Status != ApplicationStatus.Draft)
            throw new InvalidOperationException("Only draft applications can be submitted.");

        Status = ApplicationStatus.Submitted;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
    }
}
