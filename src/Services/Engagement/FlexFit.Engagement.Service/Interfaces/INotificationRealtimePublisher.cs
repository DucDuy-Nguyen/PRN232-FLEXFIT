using System;
using System.Threading.Tasks;

namespace FlexFit.Engagement.Service.Interfaces;

public interface INotificationRealtimePublisher
{
    Task PublishToUserAsync(Guid userId, object notification);
    Task PublishBroadcastAsync(object notification);
}
