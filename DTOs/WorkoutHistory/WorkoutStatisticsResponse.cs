using System.Collections.Generic;

namespace Flexfit.DTOs.WorkoutHistory
{
    public class WorkoutStatisticsResponse
    {
        public int TotalWorkouts { get; set; }
        public int TotalGymSessions { get; set; }
        public int TotalClassSessions { get; set; }
        public int TotalCaloriesBurned { get; set; }
        public int TotalDurationMinutes { get; set; }
        public double AverageCaloriesPerSession { get; set; }
        public List<DailyWorkoutStatDto> WeeklyStats { get; set; } = new();
    }

    public class DailyWorkoutStatDto
    {
        public string DayOfWeek { get; set; } = null!; // e.g. "Monday", "Thứ Hai", etc.
        public int WorkoutCount { get; set; }
        public int CaloriesBurned { get; set; }
    }
}
