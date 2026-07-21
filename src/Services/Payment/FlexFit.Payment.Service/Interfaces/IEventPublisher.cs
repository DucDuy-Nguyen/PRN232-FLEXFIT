using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Service.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string streamName, string eventType, T eventPayload, Guid? correlationId = null);
    }
}
