using FlexFit.Engagement.Application.DTOs.Notifications;
using FlexFit.Engagement.Domain.Entities;

namespace FlexFit.Engagement.Application.Interfaces;

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

    // Broadcast a lightweight notification to all connected clients in a branch group (no DB writes)
    Task BroadcastToBranchAsync(Guid branchId, string title, string content, string type);

    // Broadcast class capacity update to clients viewing a specific class
    Task BroadcastClassCapacityAsync(Guid classId, int remainingSeats);

    // Push credit balance updates to a specific user (real-time)
    Task BroadcastCreditUpdateAsync(Guid userId, int newBalance);
}
