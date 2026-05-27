using System;

namespace Flexfit.DTOs.WorkoutHistory
{
    public class WorkoutHistoryDto
    {
        public Guid WorkoutHistoryId { get; set; }
        public Guid? BookingId { get; set; }
        public string WorkoutType { get; set; } = null!; // "Class" hoặc "Gym"
        public string Name { get; set; } = null!; // Tên lớp học hoặc "Tập tự do"
        public string? BranchName { get; set; }
        public string? GymName { get; set; }
        public int CaloriesBurned { get; set; }
        public int WorkoutDurationMinutes { get; set; }
        public DateTime WorkoutDate { get; set; }
    }
}
