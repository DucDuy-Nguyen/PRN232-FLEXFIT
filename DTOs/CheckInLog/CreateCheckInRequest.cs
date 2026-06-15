using System;

namespace Flexfit.DTOs.CheckInLog
{
    /// <summary>
    /// DTO gửi lên khi hệ thống hoặc nhân viên thực hiện quét dữ liệu Check-in
    /// </summary>
    public class CheckInGymRequest
    {
        public Guid? UserId { get; set; }
        public Guid? GymBookingId { get; set; }
        public Guid? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public string? QrToken { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
    }
    public class CheckInClassRequest
    {
        public Guid UserId { get; set; }
        public Guid ClassBookingId { get; set; } // Bắt buộc đối với lịch học lớp
        public required string Status { get; set; }
        public string? Message { get; set; }
    }
}
