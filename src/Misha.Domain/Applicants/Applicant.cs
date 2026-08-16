namespace Misha.Domain.Applicants;

public sealed class Applicant
{
    private Applicant() { }

    private Applicant(Guid id, string externalReference)
    {
        Id = id;
        ExternalReference = externalReference;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string ExternalReference { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Applicant Create(string externalReference)
    {
        if (string.IsNullOrWhiteSpace(externalReference))
            throw new ArgumentException("Applicant external reference is required.", nameof(externalReference));

        var normalizedReference = externalReference.Trim();
        if (normalizedReference.Length > 200)
            throw new ArgumentException("Applicant external reference must be 200 characters or fewer.", nameof(externalReference));

        return new Applicant(Guid.NewGuid(), normalizedReference);
    }
}
