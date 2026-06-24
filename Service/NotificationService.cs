using Flexfit.DTOs.Notification; // Đảm bảo đã tạo AdminCreateNotificationRequest DTO ở bước trước
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Flexfit.Hubs;

namespace Flexfit.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserRepository _userRepo; // <-- THÊM REPO ĐỂ LẤY DANH SÁCH USER
        private readonly IHubContext<NotificationHub> _hubContext;

        // Inject thêm IUserRepository và IHubContext vào Constructor
        public NotificationService(INotificationRepository notificationRepo, IUserRepository userRepo, IHubContext<NotificationHub> hubContext)
        {
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
            _hubContext = hubContext;
        }

        public async Task SendAsync(Guid userId, string title, string content, string type)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                IsRead = false,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationRepo.AddAsync(notification);
            await _notificationRepo.SaveChangesAsync();

            // Push real-time notification to connected user group
            try
            {
                var payload = new
                {
                    notification.NotificationId,
                    notification.Title,
                    notification.Content,
                    notification.Type,
                    notification.IsRead,
                    notification.CreatedAt
                };
                await _hubContext.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", payload);
            }
            catch { }
        }

        public async Task<IEnumerable<Notification>> GetMyNotificationsAsync(Guid userId)
        {
            return await _notificationRepo.GetByUserIdAsync(userId);
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var noti = await _notificationRepo.GetByIdAndUserIdAsync(notificationId, userId);
            if (noti != null)
            {
                noti.IsRead = true;
                await _notificationRepo.UpdateAsync(noti);
                await _notificationRepo.SaveChangesAsync();
            }
        }

        // Thực hiện cập nhật hàng loạt qua Repo
        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _notificationRepo.MarkAllAsReadByUserIdAsync(userId);
            await _notificationRepo.SaveChangesAsync();
        }

        // ==========================================
        // THÊM CHỨC NĂNG: ADMIN TẠO VÀ GỬI THÔNG BÁO
        // ==========================================
        public async Task<bool> SendAdminNotificationAsync(AdminCreateNotificationRequest request)
        {
            // Trường hợp 1: Admin chọn gửi đích danh cho 1 người dùng cụ thể
            if (request.UserId.HasValue)
            {
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = request.UserId.Value,
                    Title = request.Title,
                    Content = request.Content,
                    Type = request.Type,
                    IsRead = false,
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _notificationRepo.AddAsync(notification);
            }
            // Trường hợp 2: Admin không chọn UserId -> Gửi đồng loạt cho toàn hệ thống
            else
            {
                // Lấy toàn bộ danh sách User từ DB (Hãy kiểm tra lại tên hàm trong IUserRepository của bạn)
                var allUsers = await _userRepo.GetAllAsync(); 
                
                if (allUsers != null && allUsers.Any())
                {
                    foreach (var user in allUsers)
                    {
                        var notification = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId = user.UserId, // Gán Id của từng User
                            Title = request.Title,
                            Content = request.Content,
                            Type = request.Type,
                            IsRead = false,
                            CreatedAt = DateTimeHelper.GetVietnamTime()
                        };

                        // Add từng bản ghi vào Context track 
                        await _notificationRepo.AddAsync(notification);
                    }
                }
            }

            // Lưu tất cả các thay đổi vào Cơ sở dữ liệu 1 lần duy nhất để tối ưu hiệu năng
            await _notificationRepo.SaveChangesAsync();

            // Push real-time notifications
            try
            {
                if (request.UserId.HasValue)
                {
                    var payload = new
                    {
                        Title = request.Title,
                        Content = request.Content,
                        Type = request.Type,
                        CreatedAt = DateTimeHelper.GetVietnamTime()
                    };
                    await _hubContext.Clients.Group($"user-{request.UserId.Value}").SendAsync("ReceiveNotification", payload);
                }
                else
                {
                    var payload = new
                    {
                        Title = request.Title,
                        Content = request.Content,
                        Type = request.Type,
                        CreatedAt = DateTimeHelper.GetVietnamTime()
                    };
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", payload);
                }
            }
            catch { }

            return true;
        }

        public async Task BroadcastToBranchAsync(Guid branchId, string title, string content, string type)
        {
            try
            {
                var payload = new
                {
                    Title = title,
                    Content = content,
                    Type = type,
                    BranchId = branchId,
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _hubContext.Clients.Group($"branch-{branchId}").SendAsync("ReceiveNotification", payload);
            }
            catch { }
        }

        public async Task BroadcastClassCapacityAsync(Guid classId, int remainingSeats)
        {
            try
            {
                var payload = new
                {
                    ClassId = classId,
                    RemainingSeats = remainingSeats,
                    Type = "ClassCapacityUpdate",
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _hubContext.Clients.Group($"class-{classId}").SendAsync("ClassCapacityUpdated", payload);
            }
            catch { }
        }

        public async Task BroadcastCreditUpdateAsync(Guid userId, int newBalance)
        {
            try
            {
                var payload = new
                {
                    UserId = userId,
                    NewBalance = newBalance,
                    Type = "CreditUpdate",
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _hubContext.Clients.Group($"user-{userId}").SendAsync("CreditUpdated", payload);
            }
            catch { }
        }
    }
}