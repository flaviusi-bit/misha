using Misha.Domain.ManualReviews;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class ManualReviewCaseTests
{
    [Fact]
    public void Create_starts_pending_and_records_trigger()
    {
        var applicationId = Guid.NewGuid();

        var reviewCase = ManualReviewCase.Create(
            applicationId,
            "Decision.ManualReview",
            "Watchlist result requires human review.");

        Assert.Equal(ManualReviewStatus.Pending, reviewCase.Status);
        Assert.Equal(applicationId, reviewCase.ApplicationId);
        Assert.Equal("Decision.ManualReview", reviewCase.Trigger);
        Assert.Equal("Watchlist result requires human review.", reviewCase.Reason);
    }

    [Fact]
    public void Assign_moves_case_to_in_progress_and_records_actor()
    {
        var reviewCase = CreateCase();

        reviewCase.Assign("officer-001");

        Assert.Equal(ManualReviewStatus.InProgress, reviewCase.Status);
        Assert.Equal("officer-001", reviewCase.AssignedToActorReference);
        Assert.NotNull(reviewCase.AssignedAtUtc);
    }

    [Fact]
    public void Resolve_approve_records_resolution_and_actor()
    {
        var reviewCase = CreateCase();
        reviewCase.Assign("officer-001");

        reviewCase.Resolve(
            ManualReviewResolution.Approve,
            "officer-001",
            "Evidence reviewed and eligibility confirmed.");

        Assert.Equal(ManualReviewStatus.Resolved, reviewCase.Status);
        Assert.Equal(ManualReviewResolution.Approve, reviewCase.Resolution);
        Assert.Equal("officer-001", reviewCase.ResolvedByActorReference);
        Assert.Equal("Evidence reviewed and eligibility confirmed.", reviewCase.ResolutionReason);
        Assert.NotNull(reviewCase.ResolvedAtUtc);
    }

    [Fact]
    public void Resolve_requires_reason()
    {
        var reviewCase = CreateCase();

        Assert.Throws<ArgumentException>(() => reviewCase.Resolve(
            ManualReviewResolution.Refuse,
            "officer-001",
            "  "));
    }

    [Fact]
    public void Resolved_case_cannot_be_resolved_again()
    {
        var reviewCase = CreateCase();
        reviewCase.Resolve(
            ManualReviewResolution.Refuse,
            "officer-001",
            "Eligibility requirement not met.");

        Assert.Throws<InvalidOperationException>(() => reviewCase.Resolve(
            ManualReviewResolution.Approve,
            "officer-002",
            "Second decision."));
    }

    private static ManualReviewCase CreateCase() => ManualReviewCase.Create(
        Guid.NewGuid(),
        "Decision.ManualReview",
        "Manual review is required.");
}
