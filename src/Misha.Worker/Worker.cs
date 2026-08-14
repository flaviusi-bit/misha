using Misha.Application.Messaging;

namespace Misha.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MISHA event worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
                var consumer = scope.ServiceProvider.GetRequiredService<ISqsMessageConsumer>();
                var eventDispatcher = scope.ServiceProvider.GetRequiredService<EventDispatcher>();

                await outbox.DispatchPendingAsync(stoppingToken);
                await consumer.ConsumeOnceAsync(
                    (message, token) => eventDispatcher.DispatchAsync(message, token),
                    stoppingToken);

                await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Worker processing cycle failed.");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }

        logger.LogInformation("MISHA event worker stopped.");
    }
}
