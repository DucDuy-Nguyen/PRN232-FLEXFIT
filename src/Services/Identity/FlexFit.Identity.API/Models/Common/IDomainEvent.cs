using System;

namespace FlexFit.Identity.API.Models.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events are used to record things that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
