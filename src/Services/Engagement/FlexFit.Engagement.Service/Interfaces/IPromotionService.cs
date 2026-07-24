using FlexFit.Engagement.Service.DTOs.Promotions;

namespace FlexFit.Engagement.Service.Interfaces;

public interface IPromotionService
{
    Task<IEnumerable<PromotionResponse>> GetAllPromotionsAsync(bool? isActiveOnly);
    Task<PromotionResponse> GetPromotionByIdAsync(Guid id);
    Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request);
    Task<bool> DeletePromotionAsync(Guid id);
    Task<decimal> CalculateDiscountAsync(Guid promotionId, decimal originalPrice);
}

