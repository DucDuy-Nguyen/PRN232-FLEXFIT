using Microsoft.AspNetCore.SignalR;

namespace FlexFit.Engagement.Infrastructure.Hubs;

/// <summary>
/// Marker Hub class dùng trong Infrastructure để IHubContext có thể được inject.
/// Hub thực tế (kế thừa class này) được đăng ký tại tầng API.
/// </summary>
public class NotificationHub : Hub
{
}
