using Flexfit.DTOs.Notification;
using Flexfit.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface INotificationService
    {
        /// <summary>
        /// Tạo và gửi thông báo mới cho Member (Lưu vào Database)
        /// </summary>
        /// <param name="userId">Id của người nhận thông báo</param>
        /// <param name="title">Tiêu đề thông báo</param>
        /// <param name="content">Nội dung chi tiết</param>
        /// <param name="type">Loại thông báo (BookingSuccess, PaymentSuccess, ...)</param>
        Task SendAsync(Guid userId, string title, string content, string type);

        /// <summary>
        /// Lấy toàn bộ danh sách thông báo của Member hiện tại (Xếp mới nhất lên đầu)
        /// </summary>
        Task<IEnumerable<Notification>> GetMyNotificationsAsync(Guid userId);

        /// <summary>
        /// Đánh dấu một thông báo cụ thể là đã đọc
        /// </summary>
        Task MarkAsReadAsync(Guid notificationId, Guid userId);
        /// <summary>
        /// Đánh dấu đã đọc TẤT CẢ thông báo chưa đọc của Member hiện tại
        /// </summary>
        Task MarkAllAsReadAsync(Guid userId);
        Task<bool> SendAdminNotificationAsync(AdminCreateNotificationRequest request);  
    }
}