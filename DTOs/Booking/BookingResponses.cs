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
        public int OriginalCredit { get; set; }
        public int DiscountPercent { get; set; }
        public int DiscountCredit { get; set; }
        public Guid? PromotionId { get; set; }
        public DateTime BookedAt { get; set; }
        public bool HasReview { get; set; }
        public Guid? ReviewId { get; set; }

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
        public int OriginalCredit { get; set; }
        public int DiscountPercent { get; set; }
        public int DiscountCredit { get; set; }
        public Guid? PromotionId { get; set; }
        public DateTime BookedAt { get; set; }
        public bool HasReview { get; set; }
        public Guid? ReviewId { get; set; }

        // --- BỔ SUNG 2 DÒNG NÀY ĐỂ HẾT LỖI TẠI CONTROLLER ---
        public string UserEmail { get; set; } = null!;
        public string UserFullName { get; set; } = null!;
    }

    public class StaffCheckInBookingResponse
    {
        public Guid BookingId { get; set; }
        public string BookingCode { get; set; } = null!;
        public string BookingType { get; set; } = null!;
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = null!;
        public string UserFullName { get; set; } = null!;
        public Guid? SessionId { get; set; }
        public string? SessionName { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? CoachName { get; set; }
        public Guid BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? GymName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = null!;
        public string CheckInStatus { get; set; } = null!;
        public int CreditUsed { get; set; }
        public DateTime BookedAt { get; set; }
        public string? QrToken { get; set; }
    }

    public class PromotionPreviewResponse
    {
        public int OriginalCredit { get; set; }
        public int DiscountPercent { get; set; }
        public int DiscountCredit { get; set; }
        public int FinalCredit { get; set; }
        public Guid? PromotionId { get; set; }
        public string? PromotionTitle { get; set; }
        public bool HasPromotion => PromotionId.HasValue && DiscountPercent > 0 && DiscountCredit > 0;
    }
}
