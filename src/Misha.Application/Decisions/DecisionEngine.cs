using Misha.Application.Policy;

namespace Misha.Application.Decisions;

public interface IDecisionEngine
{
    DecisionResult Decide(PolicyEvaluation policyEvaluation);
}

public sealed class DefaultDecisionEngine : IDecisionEngine
{
    public DecisionResult Decide(PolicyEvaluation policyEvaluation)
    {
        return policyEvaluation.Decision switch
        {
            PolicyDecision.Eligible => new DecisionResult(DecisionOutcome.Approve, policyEvaluation.Reasons),
            PolicyDecision.Ineligible => new DecisionResult(DecisionOutcome.Refuse, policyEvaluation.Reasons),
            PolicyDecision.ManualReview => new DecisionResult(DecisionOutcome.ManualReview, policyEvaluation.Reasons),
            PolicyDecision.NotReady => new DecisionResult(DecisionOutcome.NotReady, policyEvaluation.Reasons),
            _ => throw new ArgumentOutOfRangeException(nameof(policyEvaluation), policyEvaluation.Decision, "Unknown policy decision.")
        };
    }
}

public enum DecisionOutcome
{
    NotReady = 1,
    Approve = 2,
    Refuse = 3,
    ManualReview = 4
}

public sealed record DecisionResult(
    DecisionOutcome Decision,
    IReadOnlyList<string> Reasons);
