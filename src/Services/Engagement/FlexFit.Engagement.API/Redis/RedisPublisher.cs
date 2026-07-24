using FlexFit.Engagement.Repository.Data;
using FlexFit.Engagement.Repository.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace FlexFit.Engagement.API.Redis;

public class RedisPublisher
{
    private readonly IConnectionMultiplexer _redis;

    public RedisPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task PublishAsync<T>(string channel, T message)
    {
        var subscriber = _redis.GetSubscriber();
        var json = JsonSerializer.Serialize(message);
        await subscriber.PublishAsync(RedisChannel.Literal(channel), json);
    }
}

