using FlexFit.Engagement.Infrastructure.Data;
using StackExchange.Redis;

namespace FlexFit.Engagement.Infrastructure.Redis;

public class RedisSubscriber
{
    private readonly IConnectionMultiplexer _redis;

    public RedisSubscriber(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(RedisChannel.Literal(channel), handler);
    }

    public async Task UnsubscribeAsync(string channel)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.UnsubscribeAsync(RedisChannel.Literal(channel));
    }
}
