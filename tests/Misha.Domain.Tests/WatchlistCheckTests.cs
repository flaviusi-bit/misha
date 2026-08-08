using Misha.Domain.Watchlists;

namespace Misha.Domain.Tests;

public sealed class WatchlistCheckTests
{
    [Fact]
    public void New_check_is_not_checked()
    {
        var check = WatchlistCheck.Create(Guid.NewGuid(), "test-provider");

        Assert.Equal(WatchlistDecision.NotChecked, check.Decision);
        Assert.Null(check.CheckedAtUtc);
    }

    [Theory]
    [InlineData(WatchlistDecision.Clear)]
    [InlineData(WatchlistDecision.PotentialMatch)]
    [InlineData(WatchlistDecision.ConfirmedMatch)]
    public void Complete_records_decision(WatchlistDecision decision)
    {
        var check = WatchlistCheck.Create(Guid.NewGuid(), "test-provider");

        check.Complete(decision, "MATCH-001");

        Assert.Equal(decision, check.Decision);
        Assert.Equal("MATCH-001", check.MatchReference);
        Assert.NotNull(check.CheckedAtUtc);
        Assert.Null(check.ErrorMessage);
    }

    [Fact]
    public void Failure_records_error()
    {
        var check = WatchlistCheck.Create(Guid.NewGuid(), "test-provider");

        check.Fail("Provider unavailable");

        Assert.Equal(WatchlistDecision.Error, check.Decision);
        Assert.Equal("Provider unavailable", check.ErrorMessage);
        Assert.NotNull(check.CheckedAtUtc);
    }
}
