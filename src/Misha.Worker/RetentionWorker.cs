using Misha.Application.Retention;

namespace Misha.Worker;

public sealed class RetentionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RetentionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IRetentionPurgeService>();
                var result = await service.PurgeExpiredAsync(stoppingToken);

                logger.LogInformation(
                    "Retention cycle finished. Documents={Documents}, ApplicantsAnonymized={ApplicantsAnonymized}, ApplicantsEligible={ApplicantsEligible}, DryRun={DryRun}.",
                    result.DocumentsDeleted,
                    result.ApplicantsAnonymized,
                    result.ApplicantsEligible,
                    result.DryRun);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Retention cycle failed.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }
}
