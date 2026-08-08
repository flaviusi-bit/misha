using Misha.Application.Decisions;
using Misha.Application.Policy;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class DecisionEngineTests
{
    private readonly IDecisionEngine engine = new DefaultDecisionEngine();

    [Fact]
    public void Eligible_policy_is_the_only_path_to_approval()
    {
        var result = engine.Decide(new PolicyEvaluation(
            PolicyDecision.Eligible,
            Array.Empty<string>()));

        Assert.Equal(DecisionOutcome.Approve, result.Decision);
    }

    [Fact]
    public void Ineligible_policy_becomes_refusal()
    {
        var result = engine.Decide(new PolicyEvaluation(
            PolicyDecision.Ineligible,
            new[] { "Confirmed watchlist match." }));

        Assert.Equal(DecisionOutcome.Refuse, result.Decision);
        Assert.Contains("watchlist", result.Reasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Manual_review_never_becomes_approval()
    {
        var result = engine.Decide(new PolicyEvaluation(
            PolicyDecision.ManualReview,
            new[] { "Potential watchlist match." }));

        Assert.Equal(DecisionOutcome.ManualReview, result.Decision);
    }

    [Fact]
    public void Not_ready_never_becomes_approval()
    {
        var result = engine.Decide(new PolicyEvaluation(
            PolicyDecision.NotReady,
            new[] { "Passport verification is unavailable." }));

        Assert.Equal(DecisionOutcome.NotReady, result.Decision);
    }
}
