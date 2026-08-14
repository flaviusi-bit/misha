using Misha.Application.Messaging;

namespace Misha.Infrastructure.Persistence;

public sealed class EfOutboxWriter(MishaDbContext db) : IOutboxWriter
{
    public Task AddAsync(
        Guid eventId,
        string eventType,
        Guid aggregateId,
        string payload,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = eventId,
            EventType = eventType,
            AggregateId = aggregateId,
            Payload = payload,
            OccurredAtUtc = occurredAtUtc
        });

        return Task.CompletedTask;
    }
}
