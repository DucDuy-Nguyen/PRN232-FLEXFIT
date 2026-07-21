using System;

namespace FlexFit.RedisEventBus;

public sealed record RedisEventMessage(
    string Id,
    Guid EventId,
    string EventType,
    string Payload,
    int Version,
    int RetryCount,
    DateTimeOffset CreatedAt,
    string? CorrelationId,
    string? CausationId);
