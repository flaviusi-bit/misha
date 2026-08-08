namespace Misha.Domain.Documents;

public sealed class PassportDocument
{
    private PassportDocument() { }

    private PassportDocument(
        Guid id,
        Guid applicationId,
        string documentNumber,
        string issuingCountry,
        string surname,
        string givenNames,
        DateOnly dateOfBirth,
        string nationality,
        DateOnly expiryDate)
    {
        Id = id;
        ApplicationId = applicationId;
        DocumentNumber = documentNumber;
        IssuingCountry = issuingCountry;
        Surname = surname;
        GivenNames = givenNames;
        DateOfBirth = dateOfBirth;
        Nationality = nationality;
        ExpiryDate = expiryDate;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public DocumentType DocumentType => DocumentType.Passport;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string IssuingCountry { get; private set; } = string.Empty;
    public string Surname { get; private set; } = string.Empty;
    public string GivenNames { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public string Nationality { get; private set; } = string.Empty;
    public DateOnly ExpiryDate { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static PassportDocument Create(
        Guid applicationId,
        string documentNumber,
        string issuingCountry,
        string surname,
        string givenNames,
        DateOnly dateOfBirth,
        string nationality,
        DateOnly expiryDate)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application id is required.", nameof(applicationId));
        if (string.IsNullOrWhiteSpace(documentNumber))
            throw new ArgumentException("Document number is required.", nameof(documentNumber));
        if (string.IsNullOrWhiteSpace(issuingCountry))
            throw new ArgumentException("Issuing country is required.", nameof(issuingCountry));
        if (string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("Surname is required.", nameof(surname));
        if (string.IsNullOrWhiteSpace(givenNames))
            throw new ArgumentException("Given names are required.", nameof(givenNames));
        if (string.IsNullOrWhiteSpace(nationality))
            throw new ArgumentException("Nationality is required.", nameof(nationality));
        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Date of birth must be in the past.", nameof(dateOfBirth));
        if (expiryDate <= dateOfBirth)
            throw new ArgumentException("Expiry date must be after date of birth.", nameof(expiryDate));

        return new PassportDocument(
            Guid.NewGuid(),
            applicationId,
            documentNumber.Trim().ToUpperInvariant(),
            issuingCountry.Trim().ToUpperInvariant(),
            surname.Trim(),
            givenNames.Trim(),
            dateOfBirth,
            nationality.Trim().ToUpperInvariant(),
            expiryDate);
    }

    public bool IsExpired(DateOnly asOf) => ExpiryDate < asOf;
}
