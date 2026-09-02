using Microsoft.Extensions.Options;
using Misha.Application.Notifications;
using Misha.Infrastructure.Notifications;

namespace Misha.Worker;

public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDeliveryOptions> options,
    ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private const string GenericDeliveryFailure = "Notification delivery failed.";
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MISHA notification delivery worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var configuration = options.Value;
                if (!configuration.Enabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var delivery = scope.ServiceProvider.GetRequiredService<INotificationDelivery>();
                var notifications = await repository.GetPendingAsync(
                    Math.Clamp(configuration.BatchSize, 1, 100),
                    stoppingToken);

                foreach (var notification in notifications)
                {
                    try
                    {
                        await delivery.DeliverAsync(notification, stoppingToken);
                        notification.MarkSent();
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        notification.MarkFailed(GenericDeliveryFailure);
                        logger.LogWarning(
                            exception,
                            "Notification {NotificationId} delivery failed on attempt {Attempt}.",
                            notification.Id,
                            notification.Attempts + 1);
                    }

                    await repository.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification delivery cycle failed.");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }

        logger.LogInformation("MISHA notification delivery worker stopped.");
    }
}
