using System.ComponentModel.DataAnnotations;

namespace FlexFit.Engagement.API.Models.DTOs.WorkoutHistory;

public class UpdateWorkoutHistoryRequest
{
    [Range(0, 5000, ErrorMessage = "Lượng calo tiêu hao phải nằm trong khoảng từ 0 đến 5000.")]
    public int CaloriesBurned { get; set; }

    [Range(1, 1440, ErrorMessage = "Thời lượng buổi tập phải nằm trong khoảng từ 1 đến 1440 phút.")]
    public int WorkoutDurationMinutes { get; set; }
}
