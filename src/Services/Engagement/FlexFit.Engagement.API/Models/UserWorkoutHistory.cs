namespace FlexFit.Engagement.API.Models;

public class UserWorkoutHistory
{
    public Guid WorkoutHistoryId { get; set; }
    public Guid UserId { get; set; }
    public Guid? GymBookingId { get; set; }
    public Guid? ClassBookingId { get; set; }
    public int? CaloriesBurned { get; set; }
    public int? WorkoutDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}
