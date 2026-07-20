using Grpc.Core;

namespace FlexFit.Recommendation.Grpc.Services;

public class RecommendationGrpcService : RecommendationService.RecommendationServiceBase
{
    private readonly ILogger<RecommendationGrpcService> _logger;

    public RecommendationGrpcService(ILogger<RecommendationGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<RecommendationResponse> GetWorkoutRecommendations(
        RecommendationRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Generating workout recommendations for user: {UserId}", request.UserId);

        var response = new RecommendationResponse();
        response.Recommendations.AddRange(new[]
        {
            "Thứ Hai: Push Day (Ngực, Vai, Tay sau) - Cường độ trung bình (45 phút)",
            "Thứ Tư: Pull Day (Lưng, Tay trước) - Cường độ cao (50 phút)",
            "Thứ Sáu: Leg Day (Đùi, Mông, Bắp chuối) - Cường độ cao (60 phút)",
            "Chủ Nhật: Cardio & Core (Chạy bộ nhẹ nhàng + gập bụng) (30 phút)",
            "Lời khuyên dinh dưỡng: Nạp 2.2g Protein/kg trọng lượng cơ thể, uống đủ 2.5 lít nước mỗi ngày."
        });

        return Task.FromResult(response);
    }

    public override Task<RecommendationResponse> GetClassRecommendations(
        RecommendationRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Generating class recommendations for user: {UserId}", request.UserId);

        var response = new RecommendationResponse();
        response.Recommendations.AddRange(new[]
        {
            "Lớp học Yoga Flow - Tối Thứ Ba (19:00) tại Chi nhánh Quận 1 (Giúp tăng tính linh hoạt)",
            "Lớp học Zumba Dance - Sáng Thứ Bảy (08:30) tại Chi nhánh Bình Thạnh (Tối ưu đốt calo)",
            "Lớp học HIIT đốt mỡ - Chiều Thứ Năm (17:30) tại Chi nhánh Quận 3 (Cường độ cực cao)",
            "Mẹo thời gian: Hãy đặt lịch trước 24h để đảm bảo giữ chỗ thành công."
        });

        return Task.FromResult(response);
    }
}
