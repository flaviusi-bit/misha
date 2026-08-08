using Misha.Domain.Documents;

namespace Misha.Application.Documents;

public interface IPassportVerificationProvider
{
    string Name { get; }

    Task<PassportVerificationResult> VerifyAsync(
        PassportDocument passport,
        CancellationToken cancellationToken);
}

public enum PassportVerificationDecision
{
    NotVerified = 1,
    Verified = 2,
    Rejected = 3,
    UnableToVerify = 4,
    Error = 5
}

public sealed record PassportVerificationResult(
    PassportVerificationDecision Decision,
    string? Reference = null,
    string? ErrorMessage = null);
