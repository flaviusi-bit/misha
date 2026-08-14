namespace Misha.Application.Messaging;

public interface ISqsMessageConsumer
{
    Task<bool> ConsumeOnceAsync(
        Func<SqsMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}
