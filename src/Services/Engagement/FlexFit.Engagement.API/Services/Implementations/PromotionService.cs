using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Repositories.Interfaces;
using FlexFit.Engagement.API.Helpers;
using FlexFit.Engagement.API.DTOs.Promotions;
using FlexFit.Engagement.API.Models;
using FlexFit.Engagement.API.Services.Interfaces;

namespace FlexFit.Engagement.API.Services.Implementations;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepo;

    public PromotionService(IPromotionRepository promotionRepo)
    {
        _promotionRepo = promotionRepo;
    }

    public async Task<IEnumerable<PromotionResponse>> GetAllPromotionsAsync(bool? isActiveOnly)
    {
        var now = DateTimeHelper.GetVietnamTime();
        var promotions = await _promotionRepo.GetAllAsync(isActiveOnly);
        return promotions.Select(p => MapToResponse(p, now));
    }

    public async Task<PromotionResponse> GetPromotionByIdAsync(Guid id)
    {
        var p = await _promotionRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy chương trình khuyến mãi.");
        return MapToResponse(p, DateTimeHelper.GetVietnamTime());
    }

    public async Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request)
    {
        if (request.StartDate >= request.EndDate)
            throw new ArgumentException("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

        var now = DateTimeHelper.GetVietnamTime();
        var promotion = new Promotion
        {
            PromotionId = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DiscountPercent = request.DiscountPercent,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            CreatedAt = now
        };

        await _promotionRepo.AddAsync(promotion);
        await _promotionRepo.SaveChangesAsync();
        return MapToResponse(promotion, now);
    }

    public async Task<bool> DeletePromotionAsync(Guid id)
    {
        var p = await _promotionRepo.GetByIdAsync(id);
        if (p == null) return false;
        await _promotionRepo.DeleteAsync(p);
        await _promotionRepo.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> CalculateDiscountAsync(Guid promotionId, decimal originalPrice)
    {
        var now = DateTimeHelper.GetVietnamTime();
        var promo = await _promotionRepo.GetByIdAsync(promotionId)
            ?? throw new KeyNotFoundException("Chương trình khuyến mãi không tồn tại.");

        if (!promo.IsActive || promo.StartDate > now || promo.EndDate < now)
            throw new InvalidOperationException("Chương trình khuyến mãi không khả dụng hoặc đã hết hạn.");

        return promo.DiscountPercent.HasValue
            ? originalPrice * ((decimal)promo.DiscountPercent.Value / 100)
            : 0;
    }

    private static PromotionResponse MapToResponse(Promotion p, DateTime now) => new()
    {
        PromotionId = p.PromotionId,
        Title = p.Title,
        Description = p.Description,
        DiscountPercent = p.DiscountPercent,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsActive = p.IsActive,
        Status = p.EndDate < now ? "Expired" : (p.StartDate > now ? "NotStarted" : "Active")
    };
}
