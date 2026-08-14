using Misha.Application.Messaging;
using Xunit;

namespace Misha.Integration.Tests;

public sealed class EventDispatcherTests
{
    [Fact]
    public async Task Dispatcher_routes_event_to_matching_handler()
    {
        var handler = new RecordingHandler("application.lifecycle.changed.v1");
        var store = new RecordingIdempotencyStore();
        var dispatcher = new EventDispatcher([handler], store);
        var eventId = Guid.NewGuid();
        var message = CreateMessage(eventId, "application.lifecycle.changed.v1");

        var processed = await dispatcher.DispatchAsync(message, CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(message, handler.Message);
        Assert.Equal(eventId, store.EventId);
    }

    [Fact]
    public async Task Dispatcher_rejects_missing_event_id()
    {
        var dispatcher = new EventDispatcher(
            [new RecordingHandler("application.lifecycle.changed.v1")],
            new RecordingIdempotencyStore());
        var message = new SqsMessage(
            "message-1",
            "receipt-1",
            "{}",
            new Dictionary<string, string> { ["eventType"] = "application.lifecycle.changed.v1" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(message, CancellationToken.None));
    }

    [Fact]
    public async Task Dispatcher_rejects_unknown_event_type()
    {
        var dispatcher = new EventDispatcher(
            [new RecordingHandler("application.lifecycle.changed.v1")],
            new RecordingIdempotencyStore());
        var message = CreateMessage(Guid.NewGuid(), "unknown.event.v1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(message, CancellationToken.None));
    }

    [Fact]
    public async Task Dispatcher_does_not_ack_idempotency_when_handler_fails()
    {
        var handler = new RecordingHandler("application.lifecycle.changed.v1")
        {
            Failure = new InvalidOperationException("handler failed")
        };
        var store = new RecordingIdempotencyStore();
        var dispatcher = new EventDispatcher([handler], store);

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(
            CreateMessage(Guid.NewGuid(), "application.lifecycle.changed.v1"),
            CancellationToken.None));

        Assert.True(store.HandlerInvoked);
    }

    private static SqsMessage CreateMessage(Guid eventId, string eventType) =>
        new(
            "message-1",
            "receipt-1",
            "{}",
            new Dictionary<string, string>
            {
                ["eventId"] = eventId.ToString(),
                ["eventType"] = eventType
            });

    private sealed class RecordingHandler(string eventType) : IEventHandler
    {
        public string EventType => eventType;
        public SqsMessage? Message { get; private set; }
        public Exception? Failure { get; init; }

        public Task HandleAsync(SqsMessage message, CancellationToken cancellationToken)
        {
            Message = message;
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }

    private sealed class RecordingIdempotencyStore : IEventIdempotencyStore
    {
        public Guid EventId { get; private set; }
        public bool HandlerInvoked { get; private set; }

        public async Task<bool> ExecuteOnceAsync(
            Guid eventId,
            string eventType,
            Func<CancellationToken, Task> handler,
            CancellationToken cancellationToken)
        {
            EventId = eventId;
            HandlerInvoked = true;
            await handler(cancellationToken);
            return true;
        }
    }
}
