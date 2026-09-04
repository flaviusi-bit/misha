using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public sealed class PassportVerificationService(
    IPassportRepository passports,
    IPassportVerificationProvider provider)
{
    private const string GenericProviderFailure = "Passport verification could not be completed.";

    public async Task<PassportVerificationResult> VerifyAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var passport = await passports.GetByApplicationAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Passport for application '{applicationId}' was not found.");

        if (passport.IsExpired(DateOnly.FromDateTime(DateTime.UtcNow)))
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.Rejected,
                ErrorMessage: "Passport is expired.");
        }

        try
        {
            var result = await provider.VerifyAsync(passport, cancellationToken);

            if (result.Decision is PassportVerificationDecision.NotVerified or PassportVerificationDecision.Error)
            {
                return new PassportVerificationResult(
                    PassportVerificationDecision.Error,
                    result.Reference,
                    GenericProviderFailure);
            }

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _ = ex;
            return new PassportVerificationResult(
                PassportVerificationDecision.Error,
                ErrorMessage: GenericProviderFailure);
        }
    }
}
