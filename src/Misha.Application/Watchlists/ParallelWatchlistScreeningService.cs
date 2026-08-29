using Misha.Application.Documents;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Application.Watchlists;

public sealed class ParallelWatchlistScreeningService(
    IPassportRepository passports,
    IWatchlistCheckRepository checks,
    IEnumerable<IWatchlistProvider> providers)
{
    private static readonly TimeSpan ProviderTimeout = TimeSpan.FromSeconds(10);

    private readonly IReadOnlyList<IWatchlistProvider> _providers = providers
        .GroupBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .ToArray();

    public async Task<ParallelWatchlistScreeningResult> ScreenAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var passport = await passports.GetByApplicationAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Passport for application '{applicationId}' was not found.");

        if (_providers.Count == 0)
            throw new InvalidOperationException("At least one watchlist provider must be configured.");

        var tasks = _providers.Select(provider => ScreenProviderAsync(provider, passport, applicationId, cancellationToken));
        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
            await checks.AddAsync(result.Check, cancellationToken);

        await checks.SaveChangesAsync(cancellationToken);

        var aggregation = Aggregate(results.Select(x => x.Check.Decision));
        return new ParallelWatchlistScreeningResult(
            applicationId,
            aggregation.Decision,
            aggregation.HasConflictingResults,
            results);
    }

    private static async Task<ProviderScreeningResult> ScreenProviderAsync(
        IWatchlistProvider provider,
        PassportDocument passport,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var check = WatchlistCheck.Create(applicationId, provider.Name);
        var startedAt = DateTimeOffset.UtcNow;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ProviderTimeout);

        try
        {
            var result = await provider.CheckAsync(passport, timeoutCts.Token);
            if (result.Decision is WatchlistDecision.NotChecked or WatchlistDecision.Error)
                check.Fail(result.ErrorMessage ?? "Watchlist provider returned an invalid result.");
            else
                check.Complete(result.Decision, result.MatchReference);

            return CreateProviderResult(provider.Name, check, startedAt, timedOut: false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            check.Fail($"Watchlist provider timed out after {ProviderTimeout.TotalSeconds:0} seconds.");
            return CreateProviderResult(provider.Name, check, startedAt, timedOut: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            check.Fail(ex.Message);
            return CreateProviderResult(provider.Name, check, startedAt, timedOut: false);
        }
    }

    private static ProviderScreeningResult CreateProviderResult(
        string provider,
        WatchlistCheck check,
        DateTimeOffset startedAt,
        bool timedOut) =>
        new(provider, check, DateTimeOffset.UtcNow - startedAt, timedOut);

    private static WatchlistAggregation Aggregate(IEnumerable<WatchlistDecision> decisions)
    {
        var values = decisions.ToArray();
        var hasMatch = values.Any(decision => decision is WatchlistDecision.PotentialMatch or WatchlistDecision.ConfirmedMatch);
        var hasClear = values.Any(decision => decision == WatchlistDecision.Clear);
        var hasConflictingResults = hasMatch && hasClear;

        if (values.Any(decision => decision == WatchlistDecision.ConfirmedMatch))
            return new(WatchlistDecision.ConfirmedMatch, hasConflictingResults);
        if (values.Any(decision => decision == WatchlistDecision.PotentialMatch))
            return new(WatchlistDecision.PotentialMatch, hasConflictingResults);
        if (values.Any(decision => decision == WatchlistDecision.Error))
            return new(WatchlistDecision.Error, false);
        return new(WatchlistDecision.Clear, false);
    }
}

public sealed record ProviderScreeningResult(
    string Provider,
    WatchlistCheck Check,
    TimeSpan Duration,
    bool TimedOut);

public sealed record ParallelWatchlistScreeningResult(
    Guid ApplicationId,
    WatchlistDecision Decision,
    bool HasConflictingResults,
    IReadOnlyList<ProviderScreeningResult> Providers);

public sealed record WatchlistAggregation(
    WatchlistDecision Decision,
    bool HasConflictingResults);
