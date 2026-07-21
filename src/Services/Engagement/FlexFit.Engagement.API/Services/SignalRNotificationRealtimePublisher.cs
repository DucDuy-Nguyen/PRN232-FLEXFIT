using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using FlexFit.Engagement.API.Hubs;
using FlexFit.Engagement.Service.Interfaces;

namespace FlexFit.Engagement.API.Services;

public class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationRealtimePublisher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishToUserAsync(Guid userId, object notification)
    {
        return _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
    }

    public Task PublishBroadcastAsync(object notification)
    {
        return _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
    }
}
