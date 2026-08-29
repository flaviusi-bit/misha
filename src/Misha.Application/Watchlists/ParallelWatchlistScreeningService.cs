using Misha.Application.Documents;
using Misha.Domain.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Application.Watchlists;

public sealed class ParallelWatchlistScreeningService(
    IPassportRepository passports,
    IWatchlistCheckRepository checks,
    IEnumerable<IWatchlistProvider> providers)
{
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

        var decision = Aggregate(results.Select(x => x.Check.Decision));
        return new ParallelWatchlistScreeningResult(applicationId, decision, results);
    }

    private static async Task<ProviderScreeningResult> ScreenProviderAsync(
        IWatchlistProvider provider,
        PassportDocument passport,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var check = WatchlistCheck.Create(applicationId, provider.Name);

        try
        {
            var result = await provider.CheckAsync(passport, cancellationToken);
            if (result.Decision is WatchlistDecision.NotChecked or WatchlistDecision.Error)
                check.Fail(result.ErrorMessage ?? "Watchlist provider returned an invalid result.");
            else
                check.Complete(result.Decision, result.MatchReference);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            check.Fail(ex.Message);
        }

        return new ProviderScreeningResult(provider.Name, check);
    }

    private static WatchlistDecision Aggregate(IEnumerable<WatchlistDecision> decisions)
    {
        var values = decisions.ToArray();
        if (values.Any(decision => decision == WatchlistDecision.ConfirmedMatch))
            return WatchlistDecision.ConfirmedMatch;
        if (values.Any(decision => decision == WatchlistDecision.PotentialMatch))
            return WatchlistDecision.PotentialMatch;
        if (values.Any(decision => decision == WatchlistDecision.Error))
            return WatchlistDecision.Error;
        return WatchlistDecision.Clear;
    }
}

public sealed record ProviderScreeningResult(string Provider, WatchlistCheck Check);

public sealed record ParallelWatchlistScreeningResult(
    Guid ApplicationId,
    WatchlistDecision Decision,
    IReadOnlyList<ProviderScreeningResult> Providers);
