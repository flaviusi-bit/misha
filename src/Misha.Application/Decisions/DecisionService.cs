using System.Text.Json;
using Misha.Application.Applications;
using Misha.Application.ManualReviews;
using Misha.Application.Policy;
using Misha.Domain.Decisions;
using Misha.Domain.ManualReviews;

namespace Misha.Application.Decisions;

public sealed class DecisionService(
    IApplicationRepository applications,
    PolicyService policyService,
    IDecisionEngine decisionEngine,
    IDecisionAuditRepository audits,
    IManualReviewRepository manualReviews)
{
    public const string PolicyVersion = "1.0";

    public async Task<DecisionResult> DecideAsync(
        Guid applicationId,
        string actorReference,
        CancellationToken cancellationToken)
    {
        var application = await applications.GetAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Application '{applicationId}' was not found.");

        var policyEvaluation = await policyService.EvaluateAsync(applicationId, cancellationToken);
        var result = decisionEngine.Decide(policyEvaluation);

        switch (result.Decision)
        {
            case DecisionOutcome.Approve:
                application.Approve();
                break;
            case DecisionOutcome.Refuse:
                application.Refuse(BuildRefusalReason(result.Reasons));
                break;
            case DecisionOutcome.ManualReview:
            {
                var existingReview = await manualReviews.GetOpenForApplicationAsync(applicationId, cancellationToken);
                if (existingReview is null)
                {
                    var reason = BuildReviewReason(result.Reasons);
                    var reviewCase = ManualReviewCase.Create(
                        applicationId,
                        "Decision.ManualReview",
                        reason);
                    await manualReviews.AddAsync(reviewCase, cancellationToken);
                }

                break;
            }
            case DecisionOutcome.NotReady:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result.Decision));
        }

        var audit = DecisionAudit.Create(
            applicationId,
            PolicyVersion,
            policyEvaluation.Decision.ToString(),
            result.Decision.ToString(),
            JsonSerializer.Serialize(result.Reasons),
            actorReference);

        await audits.AddAsync(audit, cancellationToken);
        await applications.SaveChangesAsync(cancellationToken);

        return result;
    }

    private static string BuildRefusalReason(IReadOnlyList<string> reasons)
    {
        if (reasons.Count == 0)
            return "Application is not eligible under the active policy.";

        return string.Join(" ", reasons);
    }

    private static string BuildReviewReason(IReadOnlyList<string> reasons)
    {
        if (reasons.Count == 0)
            return "The active policy requires human review.";

        return string.Join(" ", reasons);
    }
}
