using Misha.Application.Documents;
using Misha.Domain.Applications;
using Misha.Domain.Watchlists;

namespace Misha.Application.Policy;

public sealed class DefaultPolicyEngine : IPolicyEngine
{
    public PolicyEvaluation Evaluate(PolicyContext context)
    {
        var reasons = new List<string>();

        if (context.ApplicationStatus != ApplicationStatus.Processing)
            reasons.Add("Application must be in Processing status.");

        if (!context.HasPassport)
            reasons.Add("A passport is required.");
        else if (context.PassportExpired)
            reasons.Add("Passport is expired.");

        switch (context.PassportVerificationDecision)
        {
            case PassportVerificationDecision.Verified:
                break;
            case PassportVerificationDecision.Rejected:
                reasons.Add("Passport verification was rejected.");
                break;
            case PassportVerificationDecision.UnableToVerify:
                reasons.Add("Passport verification is unavailable.");
                break;
            case PassportVerificationDecision.Error:
                reasons.Add("Passport verification returned an error.");
                break;
            default:
                reasons.Add("Passport verification has not completed.");
                break;
        }

        switch (context.WatchlistDecision)
        {
            case WatchlistDecision.Clear:
                break;
            case WatchlistDecision.PotentialMatch:
                reasons.Add("Watchlist screening requires manual review.");
                break;
            case WatchlistDecision.ConfirmedMatch:
                reasons.Add("Watchlist screening found a confirmed match.");
                break;
            case WatchlistDecision.Error:
                reasons.Add("Watchlist screening returned an error.");
                break;
            default:
                reasons.Add("Watchlist screening has not completed.");
                break;
        }

        if (context.WatchlistDecision == WatchlistDecision.ConfirmedMatch ||
            context.PassportVerificationDecision == PassportVerificationDecision.Rejected)
        {
            return new PolicyEvaluation(PolicyDecision.Ineligible, reasons);
        }

        if (context.WatchlistDecision == WatchlistDecision.PotentialMatch)
            return new PolicyEvaluation(PolicyDecision.ManualReview, reasons);

        if (context.PassportVerificationDecision is PassportVerificationDecision.UnableToVerify or PassportVerificationDecision.Error ||
            context.WatchlistDecision == WatchlistDecision.Error)
        {
            return new PolicyEvaluation(PolicyDecision.NotReady, reasons);
        }

        if (reasons.Count > 0)
            return new PolicyEvaluation(PolicyDecision.NotReady, reasons);

        return new PolicyEvaluation(PolicyDecision.Eligible, Array.Empty<string>());
    }
}
