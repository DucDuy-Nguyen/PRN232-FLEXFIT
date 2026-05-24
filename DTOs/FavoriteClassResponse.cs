using System;

namespace Flexfit.DTOs
{
    public class FavoriteClassResponse
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = null!;
        public string? CoachName { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int CreditCost { get; set; }
        public DateTime LikedAt { get; set; }
    }
}