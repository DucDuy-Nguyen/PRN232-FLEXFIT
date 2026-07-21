using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.API.Infrastructure.Redis.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string streamName, string eventType, T eventPayload, Guid? correlationId = null);
    }
}



