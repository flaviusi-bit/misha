namespace Misha.Application.Messaging;

public sealed class EventDispatcher(
    IEnumerable<IEventHandler> handlers,
    IEventIdempotencyStore idempotencyStore)
{
    private readonly IReadOnlyDictionary<string, IEventHandler> handlers =
        handlers.ToDictionary(x => x.EventType, StringComparer.Ordinal);

    public async Task<bool> DispatchAsync(
        SqsMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!message.Attributes.TryGetValue("eventId", out var eventIdValue) ||
            !Guid.TryParse(eventIdValue, out var eventId))
        {
            throw new InvalidOperationException("SQS message is missing a valid eventId attribute.");
        }

        if (!message.Attributes.TryGetValue("eventType", out var eventType) ||
            string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidOperationException("SQS message is missing an eventType attribute.");
        }

        if (!handlers.TryGetValue(eventType, out var handler))
        {
            throw new InvalidOperationException(
                $"No event handler is registered for event type '{eventType}'.");
        }

        return await idempotencyStore.ExecuteOnceAsync(
            eventId,
            eventType,
            token => handler.HandleAsync(message, token),
            cancellationToken);
    }
}
