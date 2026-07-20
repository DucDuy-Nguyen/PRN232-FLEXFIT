namespace FlexFit.Engagement.Application.DTOs.Reviews;

public class ReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public Guid BookingId { get; set; }
    public Guid? GymId { get; set; }
    public Guid? ClassId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
