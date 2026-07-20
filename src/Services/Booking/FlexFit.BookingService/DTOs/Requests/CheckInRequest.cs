using System;

namespace FlexFit.BookingService.DTOs.Requests
{
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
        public Guid ClassBookingId { get; set; }
        public required string Status { get; set; }
        public string? Message { get; set; }
    }
}
