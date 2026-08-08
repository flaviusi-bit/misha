using Misha.Application.Documents;
using Misha.Application.Policy;
using Misha.Domain.Applications;
using Misha.Domain.Watchlists;
using Xunit;

namespace Misha.Domain.Tests;

public sealed class PolicyEngineTests
{
    private readonly IPolicyEngine engine = new DefaultPolicyEngine();

    [Fact]
    public void All_required_checks_clear_makes_application_eligible()
    {
        var result = engine.Evaluate(new PolicyContext(
            ApplicationStatus.Processing,
            HasPassport: true,
            PassportExpired: false,
            PassportVerificationDecision.Verified,
            WatchlistDecision.Clear));

        Assert.Equal(PolicyDecision.Eligible, result.Decision);
        Assert.Empty(result.Reasons);
    }

    [Fact]
    public void Confirmed_watchlist_match_is_ineligible()
    {
        var result = engine.Evaluate(new PolicyContext(
            ApplicationStatus.Processing,
            true,
            false,
            PassportVerificationDecision.Verified,
            WatchlistDecision.ConfirmedMatch));

        Assert.Equal(PolicyDecision.Ineligible, result.Decision);
        Assert.Contains("confirmed match", result.Reasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Potential_watchlist_match_requires_manual_review()
    {
        var result = engine.Evaluate(new PolicyContext(
            ApplicationStatus.Processing,
            true,
            false,
            PassportVerificationDecision.Verified,
            WatchlistDecision.PotentialMatch));

        Assert.Equal(PolicyDecision.ManualReview, result.Decision);
    }

    [Fact]
    public void Unavailable_provider_does_not_make_application_eligible()
    {
        var result = engine.Evaluate(new PolicyContext(
            ApplicationStatus.Processing,
            true,
            false,
            PassportVerificationDecision.UnableToVerify,
            WatchlistDecision.Clear));

        Assert.Equal(PolicyDecision.NotReady, result.Decision);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public void Expired_passport_does_not_make_application_eligible()
    {
        var result = engine.Evaluate(new PolicyContext(
            ApplicationStatus.Processing,
            true,
            true,
            PassportVerificationDecision.Verified,
            WatchlistDecision.Clear));

        Assert.Equal(PolicyDecision.NotReady, result.Decision);
        Assert.Contains("expired", result.Reasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_passport_does_not_make_application_eligible()
    {
        var result = engine.Evaluate(new PolicyContext(
            ApplicationStatus.Processing,
            HasPassport: false,
            PassportExpired: false,
            PassportVerificationDecision.NotVerified,
            WatchlistDecision.Clear));

        Assert.Equal(PolicyDecision.NotReady, result.Decision);
        Assert.Contains("passport", result.Reasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Application_outside_processing_is_not_ready()
    {
        var result = engine.Evaluate(new PolicyContext(
            ApplicationStatus.Submitted,
            true,
            false,
            PassportVerificationDecision.Verified,
            WatchlistDecision.Clear));

        Assert.Equal(PolicyDecision.NotReady, result.Decision);
        Assert.Contains("Processing", result.Reasons.Single());
    }
}
