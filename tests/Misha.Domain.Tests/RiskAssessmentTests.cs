using Misha.Application.Policy;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class RiskAssessmentTests
{
    private readonly IRiskAssessmentEngine engine = new DeterministicRiskAssessmentEngine();

    [Theory]
    [InlineData(PolicyDecision.Eligible, 0, RiskLevel.Low, RiskAction.Proceed)]
    [InlineData(PolicyDecision.NotReady, 1, RiskLevel.Medium, RiskAction.EnhancedChecks)]
    [InlineData(PolicyDecision.ManualReview, 2, RiskLevel.High, RiskAction.ManualReview)]
    [InlineData(PolicyDecision.Ineligible, 3, RiskLevel.Critical, RiskAction.Escalate)]
    public void Policy_outcome_maps_to_deterministic_triage(
        PolicyDecision decision,
        int expectedScore,
        RiskLevel expectedLevel,
        RiskAction expectedAction)
    {
        var result = engine.Assess(new PolicyEvaluation(decision, new[] { "evidence" }));

        Assert.Equal(expectedScore, result.SeverityScore);
        Assert.Equal(expectedLevel, result.Level);
        Assert.Equal(expectedAction, result.RecommendedAction);
        Assert.Equal(new[] { "evidence" }, result.Indicators);
    }

    [Fact]
    public void Eligible_result_has_no_indicators()
    {
        var result = engine.Assess(new PolicyEvaluation(PolicyDecision.Eligible, Array.Empty<string>()));

        Assert.Equal(0, result.SeverityScore);
        Assert.Equal(RiskLevel.Low, result.Level);
        Assert.Equal(RiskAction.Proceed, result.RecommendedAction);
        Assert.Empty(result.Indicators);
    }

    [Fact]
    public void Null_policy_evaluation_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => engine.Assess(null!));
    }
}
