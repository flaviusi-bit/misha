using Microsoft.EntityFrameworkCore;
using Misha.Application.Messaging;
using Misha.Infrastructure.Persistence;

namespace Misha.Infrastructure.Messaging;

public sealed class EfEventIdempotencyStore(MishaDbContext db) : IEventIdempotencyStore
{
    public async Task<bool> ExecuteOnceAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(handler);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO processed_events (event_id, event_type, processed_at_utc)
            VALUES ({eventId}, {eventType}, {DateTimeOffset.UtcNow})
            ON CONFLICT (event_id) DO NOTHING;
            """, cancellationToken);

        if (inserted == 0)
        {
            var existingType = await db.Database.SqlQuery<string>($"""
                SELECT event_type AS "Value"
                FROM processed_events
                WHERE event_id = {eventId}
                LIMIT 1
                """).SingleAsync(cancellationToken);

            if (!string.Equals(existingType, eventType, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Event id '{eventId}' was already processed as '{existingType}', not '{eventType}'.");
            }

            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await handler(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
