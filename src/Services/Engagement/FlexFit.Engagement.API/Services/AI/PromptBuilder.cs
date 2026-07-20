using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.DTOs.AI;

namespace FlexFit.Engagement.API.Services.AI;

public static class PromptBuilder
{
    public static string BuildSystemInstructions()
    {
        return "Bạn là một trợ lý AI huấn luyện thể hình và dinh dưỡng chuyên nghiệp từ FlexFit.\n" +
               "Nhiệm vụ của bạn là tư vấn các lịch tập luyện, bài tập, dinh dưỡng, lối sống lành mạnh.\n" +
               "Hãy trả lời một cách khoa học, ngắn gọn, dễ hiểu và truyền động lực.";
    }

    public static string BuildUserContextPrompt(AIUserContextDto context)
    {
        return $"[Thông tin học viên]\n" +
               $"- Tên: {context.FullName}\n" +
               $"- Giới tính: {context.Gender ?? "Chưa cung cấp"}\n" +
               $"- Tuổi: {context.Age}\n" +
               $"- Chiều cao: {context.HeightCm?.ToString() ?? "Chưa cung cấp"} cm\n" +
               $"- Cân nặng: {context.WeightKg?.ToString() ?? "Chưa cung cấp"} kg\n" +
               $"- Mục tiêu tập luyện: {context.FitnessGoal ?? "Chưa cung cấp"}\n" +
               $"- Mức độ vận động: {context.ActivityLevel ?? "Chưa cung cấp"}\n" +
               $"- Thời gian tập luyện ưa thích: {context.PreferredWorkoutTime ?? "Chưa cung cấp"}\n" +
               $"- Giới thiệu bản thân: {context.Bio ?? "Chưa cung cấp"}\n";
    }

    public static string BuildWorkoutRecommendationPrompt(string rawRecommendations)
    {
        return $"Dưới đây là dữ liệu gợi ý thô về bài tập dựa trên lịch sử tập luyện của người dùng từ thuật toán thông minh:\n" +
               $"\"\"\"\n{rawRecommendations}\n\"\"\"\n" +
               $"Hãy đóng vai là huấn luyện viên cá nhân và viết một kế hoạch tập gym chi tiết, thân thiện gửi đến người dùng.";
    }

    public static string BuildClassRecommendationPrompt(string rawRecommendations)
    {
        return $"Dưới đây là dữ liệu gợi ý thô về lớp học thể hình (Yoga, Zumba, Boxing...) dựa trên sở thích và lịch sử check-in của người dùng:\n" +
               $"\"\"\"\n{rawRecommendations}\n\"\"\"\n" +
               $"Hãy đóng vai là tư vấn viên lớp học và viết lời khuyên thân thiện giới thiệu cho người dùng lý do nên tham gia lớp này.";
    }
}
