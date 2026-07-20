using System.Threading;
using System.Threading.Tasks;
using FlexFit.Contracts;

namespace FlexFit.RedisEventBus;

public interface IRedisEventPublisher
{
    Task<string> PublishAsync<TEvent>(
        string stream,
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}
