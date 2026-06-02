using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Flexfit.DTOs.AI;
using Flexfit.Models;
using Flexfit.Service.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Flexfit.Service;

public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly FlexFitDbContext _context;
    private readonly IAIContextBuilder _contextBuilder;
    private readonly string _apiKey;
    private readonly string _model;

    public AIService(HttpClient httpClient, FlexFitDbContext context, IAIContextBuilder contextBuilder, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _contextBuilder = contextBuilder;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
    }

    public async Task<AISuggestionResponse> GetWorkoutSuggestionAsync(Guid userId)
    {
        // 1. Fetch User and Profile Info
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng này.");
        }

        var profile = await _context.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        
        // 2. Fetch Recent Workouts
        var recentWorkouts = await _context.UserWorkoutHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(10)
            .Include(h => h.ClassBooking)
                .ThenInclude(cb => cb!.Class)
            .Include(h => h.GymBooking)
            .ToListAsync();

        // 3. Construct prompt
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Bạn là một huấn luyện viên cá nhân (PT) và chuyên gia dinh dưỡng chuyên nghiệp.");
        promptBuilder.AppendLine("Dựa trên thông tin thể trạng và lịch sử tập luyện của hội viên dưới đây, hãy đưa ra một gợi ý lịch tập, bài tập chi tiết và chế độ dinh dưỡng phù hợp trong tuần tới.");
        promptBuilder.AppendLine("Lời khuyên cần chi tiết, khoa học, có động lực, sử dụng tiếng Việt tự nhiên và định dạng Markdown đẹp mắt.");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("### Thông tin hội viên:");
        promptBuilder.AppendLine($"- Tên hội viên: {user.FullName}");
        if (profile != null)
        {
            promptBuilder.AppendLine($"- Giới tính: {profile.Gender ?? "Chưa cập nhật"}");
            promptBuilder.AppendLine($"- Chiều cao: {profile.HeightCm} cm");
            promptBuilder.AppendLine($"- Cân nặng: {profile.WeightKg} kg");
            promptBuilder.AppendLine($"- Mục tiêu thể hình: {profile.FitnessGoal ?? "Chưa cập nhật"}");
            promptBuilder.AppendLine($"- Mức độ hoạt động: {profile.ActivityLevel ?? "Chưa cập nhật"}");
            promptBuilder.AppendLine($"- Thời gian tập luyện ưa thích: {profile.PreferredWorkoutTime ?? "Chưa cập nhật"}");
            promptBuilder.AppendLine($"- Ghi chú cá nhân: {profile.Bio ?? "Không có"}");
        }
        else
        {
            promptBuilder.AppendLine("- Chưa cập nhật hồ sơ thể chất (Chiều cao, cân nặng, mục tiêu).");
        }
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("### Lịch sử 10 buổi tập gần đây:");
        if (recentWorkouts.Any())
        {
            foreach (var workout in recentWorkouts)
            {
                string type = workout.ClassBookingId.HasValue ? $"Lớp học: {workout.ClassBooking?.Class?.ClassName}" : "Tập tự do tại phòng Gym";
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
        promptBuilder.AppendLine("2. **Gợi ý lịch tập luyện cá nhân hóa chi tiết cho tuần tiếp theo** (Phân bổ các ngày tập, bài tập, thời gian)");
        promptBuilder.AppendLine("3. **Lời khuyên về Dinh dưỡng & Nghỉ ngơi** (Calories nạp vào ước tính, tỉ lệ protein/carb/fat khuyên dùng)");
        promptBuilder.AppendLine("4. **Lời khuyên đặc biệt phòng tránh chấn thương và giữ động lực**");

        var responseText = await CallGeminiApiAsync(promptBuilder.ToString());

        return new AISuggestionResponse
        {
            Suggestion = responseText,
            SuggestedAt = DateTime.UtcNow
        };
    }

    public async Task<AISuggestionResponse> GetClassSuggestionAsync(Guid userId)
    {
        // 1. Fetch User Profile
        var profile = await _context.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        // 2. Fetch Favorite Classes & Gyms
        var favClasses = await _context.FavoriteClasses
            .Where(f => f.UserId == userId)
            .Include(f => f.Class)
            .Select(f => f.Class.ClassName)
            .ToListAsync();

        // 3. Fetch Available Classes in System
        var availableClasses = await _context.Classes
            .Where(c => c.Status == "Active" && c.StartTime > DateTime.UtcNow)
            .OrderBy(c => c.StartTime)
            .Take(15)
            .Include(c => c.Branch)
            .Include(c => c.Category)
            .ToListAsync();

        // 4. Construct prompt
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Bạn là một chuyên viên tư vấn lớp học thể thao của hệ thống phòng tập FlexFit.");
        promptBuilder.AppendLine("Nhiệm vụ của bạn là phân tích sở thích và mục tiêu của hội viên để gợi ý những lớp học phù hợp nhất từ danh sách lớp học đang mở.");
        promptBuilder.AppendLine("Hãy trả lời bằng tiếng Việt, thân thiện, mang tính thuyết phục cao và định dạng Markdown rõ ràng.");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("### Thông tin hội viên:");
        if (profile != null)
        {
            promptBuilder.AppendLine($"- Mục tiêu thể hình: {profile.FitnessGoal ?? "Chưa cập nhật"}");
            promptBuilder.AppendLine($"- Mức độ hoạt động: {profile.ActivityLevel ?? "Chưa cập nhật"}");
        }
        else
        {
            promptBuilder.AppendLine("- Chưa có thông tin hồ sơ.");
        }
        
        if (favClasses.Any())
        {
            promptBuilder.AppendLine($"- Các lớp học yêu thích: {string.Join(", ", favClasses)}");
        }
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("### Danh sách các lớp học sắp khai giảng:");
        if (availableClasses.Any())
        {
            foreach (var c in availableClasses)
            {
                promptBuilder.AppendLine($"- **{c.ClassName}** (Mã: {c.ClassId}) | Thể loại: {c.Category.CategoryName} | Chi nhánh: {c.Branch.BranchName} | HLV: {c.CoachName ?? "Chưa phân công"} | Bắt đầu: {c.StartTime:dd/MM/yyyy HH:mm} | Độ khó: {c.DifficultyLevel ?? "Mọi cấp độ"} | Chi phí: {c.CreditCost} credits");
            }
        }
        else
        {
            promptBuilder.AppendLine("- Hiện tại chưa có lớp học nào sắp khai giảng.");
        }
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Hãy đưa ra:");
        promptBuilder.AppendLine("1. **Top 3 Lớp Học Đề Xuất Phù Hợp Nhất** kèm theo lý do cụ thể tại sao lớp học đó phù hợp với họ.");
        promptBuilder.AppendLine("2. **Mẹo sắp xếp thời gian biểu tập luyện** để tối ưu hóa hiệu quả khi tham gia các lớp học này.");

        var responseText = await CallGeminiApiAsync(promptBuilder.ToString());

        return new AISuggestionResponse
        {
            Suggestion = responseText,
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
            return "### ⚠️ Cấu hình API Chưa Sẵn Sàng\n\nQuản trị viên chưa cấu hình `Gemini:ApiKey` trong `appsettings.json`. Vui lòng thêm Gemini API Key của bạn để trải nghiệm tính năng gợi ý AI cá nhân hóa.";
        }

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            // Xây dựng contents payload cho Gemini API
            var contents = new List<object>();

            // Nếu có lịch sử chat, ta truyền thêm ngữ cảnh lịch sử
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

            // Thêm câu hỏi/prompt hiện tại
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = prompt } }
            });

            var payload = new { contents = contents };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return $"### ❌ Lỗi kết nối Gemini API\n\nHệ thống không thể kết nối tới dịch vụ AI của Google. Chi tiết lỗi: HTTP {response.StatusCode} - {errorText}";
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            
            // Trích xuất text từ cấu trúc JSON phản hồi của Gemini API
            // Response format: { "candidates": [ { "content": { "parts": [ { "text": "..." } ] } } ] }
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
            return $"### 💥 Lỗi không xác định\n\nĐã xảy ra lỗi trong quá trình xử lý yêu cầu AI: {ex.Message}";
        }
    }
}
