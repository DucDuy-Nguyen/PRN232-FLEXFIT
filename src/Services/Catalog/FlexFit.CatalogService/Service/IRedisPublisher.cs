using System.Threading.Tasks;

namespace FlexFit.CatalogService.Service;

public interface IRedisPublisher
{
    Task PublishAsync<T>(string streamName, string eventType, T eventData);
}
