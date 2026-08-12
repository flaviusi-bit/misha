using Misha.Domain.Documents;
using Misha.Domain.Watchlists;
using Misha.Infrastructure.Watchlists;

namespace Misha.Domain.Tests;

public sealed class MockWatchlistProviderTests
{
    private readonly MockWatchlistProvider _provider = new();

    [Fact]
    public async Task Clear_reference_returns_clear()
    {
        var passport = CreatePassport("P123456");

        var result = await _provider.CheckAsync(passport, CancellationToken.None);

        Assert.Equal(WatchlistDecision.Clear, result.Decision);
        Assert.Null(result.MatchReference);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Potential_marker_returns_potential_match()
    {
        var passport = CreatePassport("POTENTIAL-123");

        var result = await _provider.CheckAsync(passport, CancellationToken.None);

        Assert.Equal(WatchlistDecision.PotentialMatch, result.Decision);
        Assert.StartsWith("DEV-POTENTIAL-", result.MatchReference);
    }

    [Fact]
    public async Task Confirmed_marker_returns_confirmed_match()
    {
        var passport = CreatePassport("CONFIRMED-123");

        var result = await _provider.CheckAsync(passport, CancellationToken.None);

        Assert.Equal(WatchlistDecision.ConfirmedMatch, result.Decision);
        Assert.StartsWith("DEV-CONFIRMED-", result.MatchReference);
    }

    private static PassportDocument CreatePassport(string documentNumber) =>
        PassportDocument.Create(
            Guid.NewGuid(),
            documentNumber,
            "ROU",
            "DOE",
            "JOHN",
            new DateOnly(1990, 1, 1),
            "ROU",
            new DateOnly(2030, 1, 1));
}
