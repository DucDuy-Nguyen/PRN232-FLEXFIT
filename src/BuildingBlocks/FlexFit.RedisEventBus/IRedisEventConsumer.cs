using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.RedisEventBus;

public interface IRedisEventConsumer
{
    Task EnsureConsumerGroupAsync(
        string stream,
        string consumerGroup,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RedisEventMessage>> ReadAsync(
        string stream,
        string consumerGroup,
        string consumerName,
        int count,
        TimeSpan blockTime,
        CancellationToken cancellationToken = default);

    Task AcknowledgeAsync(
        string stream,
        string consumerGroup,
        string messageId,
        CancellationToken cancellationToken = default);
}
