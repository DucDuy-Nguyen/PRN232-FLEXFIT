namespace Flexfit.DTOs.Booking
{
    public class GymBookingResponse
    {
        public Guid BookingId { get; set; }
        public Guid SessionId { get; set; }
        public string? SessionName { get; set; }
        public string? BranchName { get; set; }
        public string? GymName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string BookingCode { get; set; } = null!;
        public string CheckInStatus { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int CreditUsed { get; set; }
        public DateTime BookedAt { get; set; }

        // --- BỔ SUNG 2 DÒNG NÀY ĐỂ HẾT LỖI TẠI CONTROLLER ---
        public string UserEmail { get; set; } = null!;
        public string UserFullName { get; set; } = null!;
    }

    public class ClassBookingResponse
    {
        public Guid BookingId { get; set; }
        public Guid ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? CoachName { get; set; }
        public string? BranchName { get; set; }
        public string? GymName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string BookingCode { get; set; } = null!;
        public string CheckInStatus { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int CreditUsed { get; set; }
        public DateTime BookedAt { get; set; }

        // --- BỔ SUNG 2 DÒNG NÀY ĐỂ HẾT LỖI TẠI CONTROLLER ---
        public string UserEmail { get; set; } = null!;
        public string UserFullName { get; set; } = null!;
    }
}