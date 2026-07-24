using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.RedisEventBus;

public interface IRedisDeadLetterPublisher
{
    Task<string> PublishAsync(
        RedisDeadLetterMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record RedisDeadLetterMessage(
    string OriginalStream,
    string OriginalMessageId,
    Guid EventId,
    string EventType,
    string Payload,
    int RetryCount,
    string ErrorSummary,
    DateTimeOffset FailedAt,
    string ConsumerGroup,
    string ConsumerName,
    string? CorrelationId);
