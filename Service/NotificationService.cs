using Flexfit.DTOs.Notification; // Đảm bảo đã tạo AdminCreateNotificationRequest DTO ở bước trước
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserRepository _userRepo; // <-- THÊM REPO ĐỂ LẤY DANH SÁCH USER

        // Inject thêm IUserRepository vào Constructor
        public NotificationService(INotificationRepository notificationRepo, IUserRepository userRepo)
        {
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
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
            return true;
        }
    }
}