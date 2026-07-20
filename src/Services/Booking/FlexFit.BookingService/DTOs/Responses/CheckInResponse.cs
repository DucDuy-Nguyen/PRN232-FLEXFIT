using System;

namespace FlexFit.BookingService.DTOs.Responses
{
    public class CheckInLogResponse
    {
        public Guid CheckInLogId { get; set; }
        public Guid UserId { get; set; }
        public string MemberName { get; set; } = null!;
        public string MemberEmail { get; set; } = null!;
        public Guid? GymBookingId { get; set; }
        public Guid? ClassBookingId { get; set; }
        public string? ClassName { get; set; }
        public Guid ScannedBy { get; set; }
        public string ScannedByName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Message { get; set; }
        public DateTime ScannedAt { get; set; }
    }
}
