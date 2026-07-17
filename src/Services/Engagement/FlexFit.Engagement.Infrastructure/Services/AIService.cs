using System.Text;
using System.Text.Json;
using FlexFit.Engagement.Application.DTOs.AI;
using FlexFit.Engagement.Application.Interfaces;
using FlexFit.Engagement.Infrastructure.Persistence;
using FlexFit.Engagement.Infrastructure.Services.AI;
using FlexFit.Recommendation.Grpc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FlexFit.Engagement.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly EngagementDbContext _context;
    private readonly IAIContextBuilder _contextBuilder;
    private readonly RecommendationService.RecommendationServiceClient _recommendationClient;
    private readonly string _apiKey;
    private readonly string _model;

    public AIService(
        HttpClient httpClient, 
        EngagementDbContext context, 
        IAIContextBuilder contextBuilder, 
        RecommendationService.RecommendationServiceClient recommendationClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _contextBuilder = contextBuilder;
        _recommendationClient = recommendationClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
    }

    public async Task<AISuggestionResponse> GetWorkoutSuggestionAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng này.");

        // 1. Gọi gRPC Recommendation Service để lấy danh sách gợi ý bài tập chuẩn
        var grpcRequest = new RecommendationRequest { UserId = userId.ToString() };
        var grpcResponse = await _recommendationClient.GetWorkoutRecommendationsAsync(grpcRequest);
        var grpcSuggestions = grpcResponse.Recommendations.ToList();

        // 2. Lấy dữ liệu lịch sử tập luyện cục bộ
        var recentWorkouts = await _context.UserWorkoutHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(10)
            .ToListAsync();

        // 3. Nếu cấu hình Gemini ApiKey, dùng AI để làm mượt văn bản và thêm chi tiết
        if (!string.IsNullOrEmpty(_apiKey))
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Bạn là một huấn luyện viên cá nhân (PT) và chuyên gia dinh dưỡng chuyên nghiệp.");
            promptBuilder.AppendLine("Dựa trên thông tin thể trạng, lịch sử tập luyện và danh sách bài tập đề xuất từ hệ thống gRPC dưới đây, hãy đưa ra một gợi ý lịch tập, bài tập chi tiết và chế độ dinh dưỡng phù hợp trong tuần tới.");
            promptBuilder.AppendLine("Lời khuyên cần chi tiết, khoa học, có động lực, sử dụng tiếng Việt tự nhiên và định dạng Markdown đẹp mắt.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("### Thông tin hội viên:");
            promptBuilder.AppendLine($"- Tên hội viên: {user.FullName}");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("### Gợi ý cơ sở từ hệ thống gRPC:");
            foreach (var sg in grpcSuggestions)
            {
                promptBuilder.AppendLine($"- {sg}");
            }
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("### Lịch sử 10 buổi tập gần đây:");
            if (recentWorkouts.Any())
            {
                foreach (var workout in recentWorkouts)
                {
                    string type = workout.ClassBookingId.HasValue ? "Lớp học" : "Tập tự do tại phòng Gym";
                    promptBuilder.AppendLine($"- Ngày: {workout.CreatedAt:dd/MM/yyyy HH:mm} | Loại hình: {type} | Thời lượng: {workout.WorkoutDurationMinutes} phút | Calo tiêu thụ ước tính: {workout.CaloriesBurned} kcal");
                }
            }
            else
            {
                promptBuilder.AppendLine("- Hội viên này chưa có lịch sử tập luyện gần đây.");
            }
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Vui lòng thiết kế cấu trúc gợi ý gồm các phần chính:");
            promptBuilder.AppendLine("1. **Đánh giá tổng quan thể trạng & quá trình tập luyện hiện tại**");
            promptBuilder.AppendLine("2. **Gợi ý lịch tập luyện cá nhân hóa chi tiết cho tuần tiếp theo** (kết hợp ý kiến từ gRPC)");
            promptBuilder.AppendLine("3. **Lời khuyên về Dinh dưỡng & Nghỉ ngơi**");
            promptBuilder.AppendLine("4. **Lời khuyên đặc biệt phòng tránh chấn thương và giữ động lực**");

            var responseText = await CallGeminiApiAsync(promptBuilder.ToString());

            return new AISuggestionResponse
            {
                Suggestion = responseText,
                SuggestedAt = DateTime.UtcNow
            };
        }

        // 4. Fallback nếu không có Gemini ApiKey: trả về trực tiếp kết quả từ gRPC
        var fallbackBuilder = new StringBuilder();
        fallbackBuilder.AppendLine("### 🤖 Gợi ý tập luyện từ gRPC Recommendation Service (Fallback)");
        fallbackBuilder.AppendLine("*(Hệ thống hiện đang chạy ở chế độ offline không dùng Gemini)*");
        fallbackBuilder.AppendLine();
        fallbackBuilder.AppendLine($"**Hội viên:** {user.FullName}");
        fallbackBuilder.AppendLine();
        fallbackBuilder.AppendLine("Dưới đây là lịch trình tập luyện được đề xuất cho bạn:");
        foreach (var sg in grpcSuggestions)
        {
            fallbackBuilder.AppendLine($"- {sg}");
        }

        return new AISuggestionResponse
        {
            Suggestion = fallbackBuilder.ToString(),
            SuggestedAt = DateTime.UtcNow
        };
    }

    public async Task<AISuggestionResponse> GetClassSuggestionAsync(Guid userId)
    {
        // 1. Gọi gRPC Recommendation Service để lấy danh sách gợi ý lớp học
        var grpcRequest = new RecommendationRequest { UserId = userId.ToString() };
        var grpcResponse = await _recommendationClient.GetClassRecommendationsAsync(grpcRequest);
        var grpcSuggestions = grpcResponse.Recommendations.ToList();

        // 2. Nếu cấu hình Gemini ApiKey, dùng AI để phân tích sâu hơn
        if (!string.IsNullOrEmpty(_apiKey))
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Bạn là một chuyên viên tư vấn lớp học thể thao của hệ thống phòng tập FlexFit.");
            promptBuilder.AppendLine("Dựa trên đề xuất lớp học từ gRPC dưới đây, hãy viết một bài tư vấn chi tiết, thuyết phục và sắp xếp thời gian hợp lý cho hội viên.");
            promptBuilder.AppendLine("Trả lời bằng tiếng Việt, thân thiện, định dạng Markdown.");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("### Đề xuất lớp học từ gRPC:");
            foreach (var sg in grpcSuggestions)
            {
                promptBuilder.AppendLine($"- {sg}");
            }
            promptBuilder.AppendLine();

            var responseText = await CallGeminiApiAsync(promptBuilder.ToString());

            return new AISuggestionResponse
            {
                Suggestion = responseText,
                SuggestedAt = DateTime.UtcNow
            };
        }

        // 3. Fallback
        var fallbackBuilder = new StringBuilder();
        fallbackBuilder.AppendLine("### 🤖 Gợi ý lớp học từ gRPC Recommendation Service (Fallback)");
        fallbackBuilder.AppendLine("*(Hệ thống hiện đang chạy ở chế độ offline không dùng Gemini)*");
        fallbackBuilder.AppendLine();
        fallbackBuilder.AppendLine("Dưới đây là các lớp học được đề xuất cho bạn:");
        foreach (var sg in grpcSuggestions)
        {
            fallbackBuilder.AppendLine($"- {sg}");
        }

        return new AISuggestionResponse
        {
            Suggestion = fallbackBuilder.ToString(),
            SuggestedAt = DateTime.UtcNow
        };
    }

    public async Task<string> ChatWithAIAsync(Guid userId, AIChatRequest request)
    {
        var userContext = await _contextBuilder.BuildUserContextAsync(userId);
        var prompt = PromptBuilder.BuildPrompt(userContext, request.Message);

        return await CallGeminiApiAsync(prompt, request.History);
    }

    private async Task<string> CallGeminiApiAsync(string prompt, List<AIChatMessage>? history = null)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "### ⚠️ Cấu hình API Chưa Sẵn Sàng\n\nQuản trị viên chưa cấu hình `Gemini:ApiKey` trong `appsettings.json`. Vui lòng thêm Gemini API Key để trải nghiệm tính năng AI.";
        }

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var contents = new List<object>();

            if (history != null && history.Any())
            {
                foreach (var msg in history)
                {
                    contents.Add(new
                    {
                        role = msg.Role == "user" ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = prompt } }
            });

            var payload = new { contents };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return $"### ❌ Lỗi kết nối Gemini API\n\nHTTP {response.StatusCode} - {errorText}";
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var contentObj) &&
                contentObj.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textProp))
            {
                return textProp.GetString() ?? "Không nhận được phản hồi hợp lệ từ AI.";
            }

            return "Không thể phân tích dữ liệu trả về từ Gemini AI.";
        }
        catch (Exception ex)
        {
            return $"### 💥 Lỗi không xác định\n\nĐã xảy ra lỗi: {ex.Message}";
        }
    }
}
