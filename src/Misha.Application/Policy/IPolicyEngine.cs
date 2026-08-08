using Misha.Application.Documents;
using Misha.Domain.Applications;
using Misha.Domain.Watchlists;

namespace Misha.Application.Policy;

public interface IPolicyEngine
{
    PolicyEvaluation Evaluate(PolicyContext context);
}

public sealed record PolicyContext(
    ApplicationStatus ApplicationStatus,
    bool HasPassport,
    bool PassportExpired,
    PassportVerificationDecision PassportVerificationDecision,
    WatchlistDecision WatchlistDecision);

public enum PolicyDecision
{
    NotReady = 1,
    Eligible = 2,
    Ineligible = 3,
    ManualReview = 4
}

public sealed record PolicyEvaluation(
    PolicyDecision Decision,
    IReadOnlyList<string> Reasons);
