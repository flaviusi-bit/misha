namespace Misha.Application.Messaging;

public interface IEventHandler
{
    string EventType { get; }

    Task HandleAsync(SqsMessage message, CancellationToken cancellationToken);
}
