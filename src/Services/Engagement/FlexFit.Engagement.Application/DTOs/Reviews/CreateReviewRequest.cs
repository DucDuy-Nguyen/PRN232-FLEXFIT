namespace FlexFit.Engagement.Application.DTOs.Reviews;

public sealed class CreateReviewRequest
{
    public Guid BookingId { get; set; }
    public Guid? GymId { get; set; }
    public Guid? ClassId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
