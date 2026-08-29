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
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Nationality { get; private set; }
    public string? CountryOfBirth { get; private set; }
    public string? PlaceOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public bool ProfileCompleted =>
        !string.IsNullOrWhiteSpace(FirstName)
        && !string.IsNullOrWhiteSpace(LastName)
        && DateOfBirth.HasValue
        && !string.IsNullOrWhiteSpace(Nationality);

    public static Applicant Create(string externalReference)
    {
        if (string.IsNullOrWhiteSpace(externalReference))
            throw new ArgumentException("Applicant external reference is required.", nameof(externalReference));

        var normalizedReference = externalReference.Trim();
        if (normalizedReference.Length > 200)
            throw new ArgumentException("Applicant external reference must be 200 characters or fewer.", nameof(externalReference));

        return new Applicant(Guid.NewGuid(), normalizedReference);
    }

    public void SetProfile(ApplicantProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var firstName = NormalizeRequired(profile.FirstName, 100, nameof(profile.FirstName));
        var lastName = NormalizeRequired(profile.LastName, 100, nameof(profile.LastName));
        var nationality = NormalizeRequired(profile.Nationality, 3, nameof(profile.Nationality)).ToUpperInvariant();

        if (profile.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Date of birth must be in the past.", nameof(profile.DateOfBirth));

        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = profile.DateOfBirth;
        Nationality = nationality;
        CountryOfBirth = NormalizeOptional(profile.CountryOfBirth, 3)?.ToUpperInvariant();
        PlaceOfBirth = NormalizeOptional(profile.PlaceOfBirth, 200);
        Gender = NormalizeOptional(profile.Gender, 32);
        Email = NormalizeOptional(profile.Email, 320);
        PhoneNumber = NormalizeOptional(profile.PhoneNumber, 50);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Anonymize()
    {
        ExternalReference = $"anonymized:{Id:N}";
        FirstName = null;
        LastName = null;
        DateOfBirth = null;
        Nationality = null;
        CountryOfBirth = null;
        PlaceOfBirth = null;
        Gender = null;
        Email = null;
        PhoneNumber = null;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Value must be {maxLength} characters or fewer.", parameterName);

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Value must be {maxLength} characters or fewer.");

        return normalized;
    }
}

public sealed record ApplicantProfile(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Nationality,
    string? CountryOfBirth,
    string? PlaceOfBirth,
    string? Gender,
    string? Email,
    string? PhoneNumber);
