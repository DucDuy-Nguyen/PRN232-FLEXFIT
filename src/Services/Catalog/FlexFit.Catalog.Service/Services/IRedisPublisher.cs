using FlexFit.Catalog.Service.Interfaces;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Service.Services;

public interface IRedisPublisher
{
    Task PublishAsync<T>(string streamName, string eventType, T eventData);
}


