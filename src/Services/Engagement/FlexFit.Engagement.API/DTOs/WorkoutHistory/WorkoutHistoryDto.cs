namespace FlexFit.Engagement.API.DTOs.WorkoutHistory;

public class WorkoutHistoryDto
{
    public Guid WorkoutHistoryId { get; set; }
    public Guid? GymBookingId { get; set; }
    public Guid? ClassBookingId { get; set; }
    public int CaloriesBurned { get; set; }
    public int WorkoutDurationMinutes { get; set; }
    public DateTime WorkoutDate { get; set; }
}
