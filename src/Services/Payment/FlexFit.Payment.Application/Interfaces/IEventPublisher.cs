using System;
using System.Threading.Tasks;

namespace FlexFit.Payment.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string streamName, string eventType, T eventPayload, Guid? correlationId = null);
    }
}
