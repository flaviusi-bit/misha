using Misha.Domain.Documents;
using Misha.Application.Applications;

namespace Misha.Application.Documents;

public sealed class PassportService(
    IApplicationRepository applications,
    IPassportRepository passports)
{
    public async Task CreateAsync(
        Guid applicationId,
        string documentNumber,
        string issuingCountry,
        string surname,
        string givenNames,
        DateOnly dateOfBirth,
        string nationality,
        DateOnly expiryDate,
        CancellationToken cancellationToken)
    {
        var application = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        var existing = await passports.GetByApplicationAsync(application.Id, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("The application already has a passport document.");

        var passport = PassportDocument.Create(
            applicationId,
            documentNumber,
            issuingCountry,
            surname,
            givenNames,
            dateOfBirth,
            nationality,
            expiryDate);

        await passports.AddAsync(passport, cancellationToken);
        await passports.SaveChangesAsync(cancellationToken);
    }

    public Task<PassportDocument?> GetAsync(Guid applicationId, CancellationToken cancellationToken) =>
        passports.GetByApplicationAsync(applicationId, cancellationToken);
}
