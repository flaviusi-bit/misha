using Misha.Application.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Application.Watchlists;

public sealed class WatchlistService(
    IPassportRepository passports,
    IWatchlistCheckRepository checks,
    IWatchlistProvider provider)
{
    private const string GenericProviderFailure = "Watchlist provider request failed.";

    public async Task<WatchlistCheck> ScreenAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var passport = await passports.GetByApplicationAsync(applicationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Passport for application '{applicationId}' was not found.");

        var check = WatchlistCheck.Create(applicationId, provider.Name);
        await checks.AddAsync(check, cancellationToken);

        try
        {
            var result = await provider.CheckAsync(passport, cancellationToken);
            if (result.Decision is WatchlistDecision.NotChecked or WatchlistDecision.Error)
            {
                check.Fail(GenericProviderFailure);
            }
            else
            {
                check.Complete(result.Decision, result.MatchReference);
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            check.Fail(GenericProviderFailure);
        }

        await checks.SaveChangesAsync(cancellationToken);
        return check;
    }

    public Task<WatchlistCheck?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
        checks.GetLatestAsync(applicationId, cancellationToken);
}
