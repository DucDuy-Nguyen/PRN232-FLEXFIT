using System;

namespace Flexfit.DTOs.Review
{
    public class ReviewResponse
    {
        public Guid ReviewId { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = null!;
        public Guid? GymId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? ClassBookingId { get; set; }
        public Guid? GymBookingId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
