using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.RedisEventBus;

public interface IRedisPendingMessageRecovery
{
    Task<IReadOnlyCollection<RedisEventMessage>> ClaimStaleMessagesAsync(
        string stream,
        string consumerGroup,
        string consumerName,
        TimeSpan minimumIdleTime,
        int count,
        CancellationToken cancellationToken = default);
}
