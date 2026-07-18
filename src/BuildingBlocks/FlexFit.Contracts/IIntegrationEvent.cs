using System;

namespace FlexFit.Contracts;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string EventType { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
    int Version { get; }
}
