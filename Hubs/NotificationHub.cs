using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;
using System.Security.Claims;
using Flexfit.Repositories;
using System;


namespace Flexfit.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class NotificationHub : Hub
    {
        private readonly IBranchRepository _branchRepo;
        private readonly IGymRepository _gymRepo;

        public NotificationHub(IBranchRepository branchRepo, IGymRepository gymRepo)
        {
            _branchRepo = branchRepo;
            _gymRepo = gymRepo;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var guid))
            {
                // add connection to a per-user group for reliable targeting
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

                // If user is staff in any branches, add to branch groups
                try
                {
                    var user = await _branchRepo.GetUserByIdAsync(guid);
                    if (user?.BranchStaffs != null)
                    {
                        foreach (var bs in user.BranchStaffs)
                        {
                            if (bs?.BranchId != null)
                            {
                                await Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{bs.BranchId}");
                            }
                        }
                    }

                    // If user owns gyms, add to owner group
                    var ownedCount = await _gymRepo.CountGymsByOwnerIdAsync(guid);
                    if (ownedCount > 0)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"owner-{userId}");
                    }
                }
                catch { }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var guid))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
                try
                {
                    var user = await _branchRepo.GetUserByIdAsync(guid);
                    if (user?.BranchStaffs != null)
                    {
                        foreach (var bs in user.BranchStaffs)
                        {
                            if (bs?.BranchId != null)
                            {
                                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"branch-{bs.BranchId}");
                            }
                        }
                    }

                    var ownedCount = await _gymRepo.CountGymsByOwnerIdAsync(guid);
                    if (ownedCount > 0)
                    {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"owner-{userId}");
                    }
                }
                catch { }
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Clients can join/leave class-specific groups to receive capacity updates
        public Task JoinClassGroup(Guid classId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, $"class-{classId}");
        }

        public Task LeaveClassGroup(Guid classId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"class-{classId}");
        }
    }
}
