namespace Misha.Application.Policy;

public interface IRiskAssessmentEngine
{
    RiskAssessment Assess(PolicyEvaluation policyEvaluation);
}

public sealed record RiskAssessment(
    int SeverityScore,
    RiskLevel Level,
    RiskAction RecommendedAction,
    IReadOnlyList<string> Indicators);

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum RiskAction
{
    Proceed = 1,
    EnhancedChecks = 2,
    ManualReview = 3,
    Escalate = 4
}
