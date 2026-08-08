using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public sealed class PassportVerificationService(
    IPassportRepository passports,
    IPassportVerificationProvider provider)
{
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

            if (result.Decision is PassportVerificationDecision.NotVerified)
            {
                return new PassportVerificationResult(
                    PassportVerificationDecision.Error,
                    result.Reference,
                    result.ErrorMessage ?? "Passport verification provider returned an incomplete result.");
            }

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new PassportVerificationResult(
                PassportVerificationDecision.Error,
                ErrorMessage: ex.Message);
        }
    }
}
