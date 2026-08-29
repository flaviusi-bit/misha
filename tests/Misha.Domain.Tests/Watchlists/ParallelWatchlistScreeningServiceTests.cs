using Misha.Application.Documents;
using Misha.Application.Watchlists;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;
using Xunit;

namespace Misha.Domain.Tests.Watchlists;

public sealed class ParallelWatchlistScreeningServiceTests
{
    [Fact]
    public async Task Providers_are_invoked_in_parallel_and_all_results_are_persisted()
    {
        var started = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var passport = CreatePassport();
        var repository = new FakePassportRepository(passport);
        var checks = new FakeWatchlistCheckRepository();
        var providers = new IWatchlistProvider[]
        {
            new DelegateProvider("provider-a", async (_, ct) =>
            {
                Interlocked.Increment(ref started);
                firstStarted.SetResult();
                await release.Task.WaitAsync(ct);
                return new WatchlistProviderResult(WatchlistDecision.Clear);
            }),
            new DelegateProvider("provider-b", async (_, ct) =>
            {
                Interlocked.Increment(ref started);
                secondStarted.SetResult();
                await release.Task.WaitAsync(ct);
                return new WatchlistProviderResult(WatchlistDecision.PotentialMatch, "B-123");
            })
        };

        var service = new ParallelWatchlistScreeningService(repository, checks, providers);
        var screeningTask = service.ScreenAsync(passport.ApplicationId, CancellationToken.None);

        await Task.WhenAll(firstStarted.Task, secondStarted.Task);
        Assert.Equal(2, Volatile.Read(ref started));

        release.SetResult();
        var result = await screeningTask;

        Assert.Equal(WatchlistDecision.PotentialMatch, result.Decision);
        Assert.True(result.HasConflictingResults);
        Assert.Equal(2, result.Providers.Count);
        Assert.Equal(2, checks.Added.Count);
        Assert.All(checks.Added, check => Assert.NotEqual(WatchlistDecision.NotChecked, check.Decision));
        Assert.All(result.Providers, provider => Assert.False(provider.TimedOut));
        Assert.All(result.Providers, provider => Assert.True(provider.Duration >= TimeSpan.Zero));
    }

    [Fact]
    public async Task Confirmed_match_has_precedence_over_potential_and_clear()
    {
        var result = await RunDecisionsAsync(
            new("clear", WatchlistDecision.Clear),
            new("potential", WatchlistDecision.PotentialMatch),
            new("confirmed", WatchlistDecision.ConfirmedMatch));

        Assert.Equal(WatchlistDecision.ConfirmedMatch, result.Decision);
        Assert.True(result.HasConflictingResults);
    }

    [Fact]
    public async Task Potential_match_has_precedence_over_clear()
    {
        var result = await RunDecisionsAsync(
            new("clear", WatchlistDecision.Clear),
            new("potential", WatchlistDecision.PotentialMatch));

        Assert.Equal(WatchlistDecision.PotentialMatch, result.Decision);
        Assert.True(result.HasConflictingResults);
    }

    [Fact]
    public async Task Matching_provider_results_are_not_marked_as_conflicting()
    {
        var result = await RunDecisionsAsync(
            new("provider-a", WatchlistDecision.Clear),
            new("provider-b", WatchlistDecision.Clear));

        Assert.Equal(WatchlistDecision.Clear, result.Decision);
        Assert.False(result.HasConflictingResults);
    }

    [Fact]
    public async Task Provider_error_never_aggregates_to_clear()
    {
        var result = await RunDecisionsAsync(
            new("clear", WatchlistDecision.Clear),
            new("failed", WatchlistDecision.Error));

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        Assert.False(result.HasConflictingResults);
    }

    [Fact]
    public async Task Provider_exception_isolated_as_error_and_other_provider_completes()
    {
        var result = await RunProvidersAsync(
            new DelegateProvider("healthy", (_, _) => Task.FromResult(new WatchlistProviderResult(WatchlistDecision.Clear))),
            new DelegateProvider("broken", (_, _) => throw new InvalidOperationException("provider unavailable")));

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        var broken = Assert.Single(result.Providers, x => x.Provider == "broken");
        Assert.Equal(WatchlistDecision.Error, broken.Check.Decision);
        Assert.Equal("provider unavailable", broken.Check.ErrorMessage);
        Assert.False(broken.TimedOut);
    }

    [Fact]
    public async Task Slow_provider_isolated_as_timeout_and_does_not_block_other_provider()
    {
        var result = await RunProvidersAsync(
            new DelegateProvider("healthy", (_, _) => Task.FromResult(new WatchlistProviderResult(WatchlistDecision.Clear))),
            new DelegateProvider("slow", async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new WatchlistProviderResult(WatchlistDecision.Clear);
            }));

        Assert.Equal(WatchlistDecision.Error, result.Decision);
        var slow = Assert.Single(result.Providers, x => x.Provider == "slow");
        Assert.Equal(WatchlistDecision.Error, slow.Check.Decision);
        Assert.True(slow.TimedOut);
        Assert.Contains("timed out", slow.Check.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        var healthy = Assert.Single(result.Providers, x => x.Provider == "healthy");
        Assert.Equal(WatchlistDecision.Clear, healthy.Check.Decision);
    }

    private static Task<ParallelWatchlistScreeningResult> RunDecisionsAsync(params (string Name, WatchlistDecision Decision)[] definitions) =>
        RunProvidersAsync(definitions.Select(x => new DelegateProvider(x.Name, (_, _) => Task.FromResult(new WatchlistProviderResult(x.Decision)))).ToArray());

    private static async Task<ParallelWatchlistScreeningResult> RunProvidersAsync(params IWatchlistProvider[] providers)
    {
        var passport = CreatePassport();
        var service = new ParallelWatchlistScreeningService(
            new FakePassportRepository(passport),
            new FakeWatchlistCheckRepository(),
            providers);
        return await service.ScreenAsync(passport.ApplicationId, CancellationToken.None);
    }

    private static PassportDocument CreatePassport() => PassportDocument.Create(
        Guid.NewGuid(), "AB123456", "ROU", "DOE", "JOHN", new DateOnly(1990, 1, 1), "ROU", new DateOnly(2030, 1, 1));

    private sealed class DelegateProvider(string name, Func<PassportDocument, CancellationToken, Task<WatchlistProviderResult>> handler) : IWatchlistProvider
    {
        public string Name => name;
        public Task<WatchlistProviderResult> CheckAsync(PassportDocument passport, CancellationToken cancellationToken) => handler(passport, cancellationToken);
    }

    private sealed class FakePassportRepository(PassportDocument passport) : IPassportRepository
    {
        public Task<PassportDocument?> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken) => Task.FromResult<PassportDocument?>(passport.ApplicationId == applicationId ? passport : null);
        public Task AddAsync(PassportDocument value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeWatchlistCheckRepository : IWatchlistCheckRepository
    {
        public List<WatchlistCheck> Added { get; } = [];
        public Task<WatchlistCheck?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) => Task.FromResult<WatchlistCheck?>(Added.LastOrDefault(x => x.ApplicationId == applicationId));
        public Task AddAsync(WatchlistCheck check, CancellationToken cancellationToken) { Added.Add(check); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
