namespace Flexfit.DTOs
{
    public class CustomerBookingHistoryResponse
    {
        public Guid BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public string BookingType { get; set; } = null!; // "GYM" hoặc "CLASS"
        public string Name { get; set; } = null!;        // Tên Session Gym hoặc Tên Lớp Class
        public string BranchName { get; set; } = null!;  // Tên chi nhánh tập
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int CreditUsed { get; set; }
        public string Status { get; set; } = null!;       // Booked, Cancelled...
        public string CheckInStatus { get; set; } = null!; // NotCheckedIn, CheckedIn
        public DateTime? CheckInTime { get; set; }
        public string? CustomerName { get; set; }  // Tên khách hàng đặt lịch
        public string? CustomerEmail { get; set; } // Email khách hàng đặt lịch
    }
}