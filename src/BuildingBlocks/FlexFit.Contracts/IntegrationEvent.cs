using System;

namespace FlexFit.Contracts;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => GetType().Name;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public int Version { get; init; } = 1;
}
