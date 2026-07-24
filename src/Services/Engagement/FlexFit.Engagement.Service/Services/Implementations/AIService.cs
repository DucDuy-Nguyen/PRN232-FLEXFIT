using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using FlexFit.Engagement.Service.DTOs.AI;
using FlexFit.Engagement.Repository.Repositories.Interfaces;
using FlexFit.Engagement.Service.Interfaces;
using FlexFit.Engagement.Service.Services.AI;

namespace FlexFit.Engagement.Service.Services
{
    public class AIService : IAIService
    {
        private readonly IEngagementUserRepository _userRepository;
        private readonly IWorkoutHistoryRepository _workoutHistoryRepository;
        private readonly IAIContextBuilder _contextBuilder;
        private readonly IAIClient _aiClient;
        private readonly IRecommendationClient _recommendationClient;

        public AIService(
            IEngagementUserRepository userRepository,
            IWorkoutHistoryRepository workoutHistoryRepository,
            IAIContextBuilder contextBuilder, 
            IAIClient aiClient,
            IRecommendationClient recommendationClient)
        {
            _userRepository = userRepository;
            _workoutHistoryRepository = workoutHistoryRepository;
            _contextBuilder = contextBuilder;
            _aiClient = aiClient;
            _recommendationClient = recommendationClient;
        }

        public async Task<AISuggestionResponse> GetWorkoutSuggestionAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Không tìm thấy người dùng này.");

            // 1. Gọi gRPC Recommendation Service để lấy danh sách gợi ý bài tập chuẩn
            var grpcSuggestions = await _recommendationClient.GetWorkoutRecommendationsAsync(userId);

            // 2. Lấy dữ liệu lịch sử tập luyện cục bộ via Repository
            var recentWorkouts = await _workoutHistoryRepository.GetRecentByUserIdAsync(userId, 10);

            // 3. Xây dựng prompt
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

            var responseText = await _aiClient.GenerateContentAsync(promptBuilder.ToString());

            // Check if API key was missing or failed - trigger fallback
            if (responseText.StartsWith("### ⚠️ Cấu hình API Chưa Sẵn Sàng"))
            {
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

            return new AISuggestionResponse
            {
                Suggestion = responseText,
                SuggestedAt = DateTime.UtcNow
            };
        }

        public async Task<AISuggestionResponse> GetClassSuggestionAsync(Guid userId)
        {
            // 1. Gọi gRPC Recommendation Service để lấy danh sách gợi ý lớp học
            var grpcSuggestions = await _recommendationClient.GetClassRecommendationsAsync(userId);

            // 2. Dùng AI để phân tích sâu hơn
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

            var responseText = await _aiClient.GenerateContentAsync(promptBuilder.ToString());

            if (responseText.StartsWith("### ⚠️ Cấu hình API Chưa Sẵn Sàng"))
            {
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

            return new AISuggestionResponse
            {
                Suggestion = responseText,
                SuggestedAt = DateTime.UtcNow
            };
        }

        public async Task<string> ChatWithAIAsync(Guid userId, AIChatRequest request)
        {
            var userContext = await _contextBuilder.GetUserContextAsync(userId);
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(PromptBuilder.BuildSystemInstructions());
            promptBuilder.AppendLine(PromptBuilder.BuildUserContextPrompt(userContext));
            promptBuilder.AppendLine($"[Yêu cầu từ người dùng]\n{request.Message}");

            return await _aiClient.GenerateContentAsync(promptBuilder.ToString(), request.History);
        }
    }
}
