namespace Misha.Application.Messaging;

public sealed record ApplicationLifecycleChanged(
    Guid EventId,
    Guid ApplicationId,
    string? FromStatus,
    string ToStatus,
    string? Reason,
    string ActorReference,
    DateTimeOffset OccurredAtUtc);
