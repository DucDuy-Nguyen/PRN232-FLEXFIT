using System;

namespace Flexfit.DTOs.Review
{
    public class CreateReviewRequest
    {
        public Guid BookingId { get; set; }
        public string BookingType { get; set; } // "Class" hoặc "Gym"
        public int Rating { get; set; } // Số sao từ 1 đến 5
        public string? Comment { get; set; }
    }
}
