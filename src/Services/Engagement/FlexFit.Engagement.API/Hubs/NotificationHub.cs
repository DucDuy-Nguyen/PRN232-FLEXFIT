using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FlexFit.Engagement.API.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationHub : FlexFit.Engagement.Infrastructure.Hubs.NotificationHub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out _))
        {
            // Add connection to a per-user group for reliable targeting
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Clients can join/leave branch-specific groups (e.g. staff)
    public Task JoinBranchGroup(Guid branchId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{branchId}");

    public Task LeaveBranchGroup(Guid branchId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch-{branchId}");

    // Clients can join/leave class-specific groups to receive capacity updates
    public Task JoinClassGroup(Guid classId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"class-{classId}");

    public Task LeaveClassGroup(Guid classId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"class-{classId}");
}
