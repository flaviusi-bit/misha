namespace Misha.Application.Policy;

/// <summary>
/// Converts an already-evaluated policy outcome into an explainable triage severity.
/// The score is ordinal severity, not a probability of wrongdoing or refusal.
/// No demographic or protected characteristics are used.
/// </summary>
public sealed class DeterministicRiskAssessmentEngine : IRiskAssessmentEngine
{
    public RiskAssessment Assess(PolicyEvaluation policyEvaluation)
    {
        ArgumentNullException.ThrowIfNull(policyEvaluation);

        return policyEvaluation.Decision switch
        {
            PolicyDecision.Eligible => new RiskAssessment(
                SeverityScore: 0,
                RiskLevel.Low,
                RiskAction.Proceed,
                policyEvaluation.Reasons),

            PolicyDecision.NotReady => new RiskAssessment(
                SeverityScore: 1,
                RiskLevel.Medium,
                RiskAction.EnhancedChecks,
                policyEvaluation.Reasons),

            PolicyDecision.ManualReview => new RiskAssessment(
                SeverityScore: 2,
                RiskLevel.High,
                RiskAction.ManualReview,
                policyEvaluation.Reasons),

            PolicyDecision.Ineligible => new RiskAssessment(
                SeverityScore: 3,
                RiskLevel.Critical,
                RiskAction.Escalate,
                policyEvaluation.Reasons),

            _ => throw new ArgumentOutOfRangeException(nameof(policyEvaluation), policyEvaluation.Decision, "Unsupported policy decision.")
        };
    }
}
