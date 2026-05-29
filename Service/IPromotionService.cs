using Flexfit.DTOs.Promotion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public interface IPromotionService
    {
        Task<IEnumerable<PromotionResponse>> GetAllPromotionsAsync(bool? isActiveOnly);
        Task<PromotionResponse> GetPromotionByIdAsync(Guid id);
        Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request);
        Task<bool> DeletePromotionAsync(Guid id);

        // Hàm tính toán số tiền giảm giá dựa trên Id chương trình và giá gốc của Credit Package
        Task<decimal> CalculateDiscountAsync(Guid promotionId, decimal originalPrice);
    }
}