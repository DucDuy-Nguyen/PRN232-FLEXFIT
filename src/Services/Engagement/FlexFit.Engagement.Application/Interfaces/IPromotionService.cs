using FlexFit.Engagement.Application.DTOs.Promotions;

namespace FlexFit.Engagement.Application.Interfaces;

public interface IPromotionService
{
    Task<IEnumerable<PromotionResponse>> GetAllPromotionsAsync(bool? isActiveOnly);
    Task<PromotionResponse> GetPromotionByIdAsync(Guid id);
    Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request);
    Task<bool> DeletePromotionAsync(Guid id);
    Task<decimal> CalculateDiscountAsync(Guid promotionId, decimal originalPrice);
}
