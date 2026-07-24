using System;

namespace FlexFit.Booking.Service.DTOs.Requests
{
    public class CreateGymBookingRequest
    {
        public required Guid BranchId { get; set; }
        public required string SessionName { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public Guid? PromotionId { get; set; }
    }

    public class CreateClassBookingRequest
    {
        public required Guid ClassId { get; set; }
        public Guid? PromotionId { get; set; }
    }
}
