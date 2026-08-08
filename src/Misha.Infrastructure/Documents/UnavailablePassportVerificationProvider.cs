using Misha.Application.Documents;
using Misha.Domain.Documents;

namespace Misha.Infrastructure.Documents;

public sealed class UnavailablePassportVerificationProvider : IPassportVerificationProvider
{
    public string Name => "not-configured";

    public Task<PassportVerificationResult> VerifyAsync(
        PassportDocument passport,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PassportVerificationResult(
            PassportVerificationDecision.UnableToVerify,
            ErrorMessage: "No passport verification provider is configured."));
}
