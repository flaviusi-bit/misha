namespace Misha.Application.Messaging;

public interface IOutboxWriter
{
    Task AddAsync(
        Guid eventId,
        string eventType,
        Guid aggregateId,
        string payload,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken);
}
