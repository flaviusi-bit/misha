namespace Misha.Application.Messaging;

public interface IEventIdempotencyStore
{
    Task<bool> ExecuteOnceAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}
