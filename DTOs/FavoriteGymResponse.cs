namespace Flexfit.DTOs
{
    public class FavoriteGymResponse
    {
        public Guid GymId { get; set; }
        public string GymName { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime LikedAt { get; set; }
    }
}