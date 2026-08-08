using Misha.Application.Documents;
using Misha.Domain.Watchlists;

namespace Misha.Application.Watchlists;

public sealed class WatchlistService(
    IPassportRepository passports,
    IWatchlistCheckRepository checks,
    IWatchlistProvider provider)
{
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
                check.Fail(result.ErrorMessage ?? "Watchlist provider returned an invalid result.");
            }
            else
            {
                check.Complete(result.Decision, result.MatchReference);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            check.Fail(ex.Message);
        }

        await checks.SaveChangesAsync(cancellationToken);
        return check;
    }

    public Task<WatchlistCheck?> GetLatestAsync(Guid applicationId, CancellationToken cancellationToken) =>
        checks.GetLatestAsync(applicationId, cancellationToken);
}
