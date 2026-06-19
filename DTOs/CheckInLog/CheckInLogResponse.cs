using System;

namespace Flexfit.DTOs.CheckInLog
{
    /// <summary>
    /// DTO cấu trúc dữ liệu trả về hiển thị lịch sử Check-in lên giao diện (Hội viên/Nhân viên)
    /// </summary>
    public class CheckInLogResponse
    {
        public Guid CheckInLogId { get; set; }
        public Guid UserId { get; set; }

        // Thông tin chi tiết của hội viên lấy từ bảng User
        public string MemberName { get; set; } = null!;
        public string MemberEmail { get; set; } = null!;

        public Guid? GymBookingId { get; set; }
        public Guid? ClassBookingId { get; set; }

        // Hiển thị tên lớp học trực quan nếu đây là lượt check-in ClassBooking
        public string? ClassName { get; set; }

        public Guid ScannedBy { get; set; }

        // Tên nhân viên thực hiện thao tác quét hoặc "Hệ thống tự động"
        public string ScannedByName { get; set; } = null!;

        public string Status { get; set; } = null!;
        public string? Message { get; set; }
        public DateTime ScannedAt { get; set; }
    }
}