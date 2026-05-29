using Flexfit.DTOs.Promotion;
using Flexfit.Models;
using Flexfit.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepo;

        public PromotionService(IPromotionRepository promotionRepo)
        {
            _promotionRepo = promotionRepo;
        }

        /// <summary>
        /// Lấy thời gian hiện tại theo múi giờ Việt Nam (GMT+7)
        /// </summary>
        private DateTime GetVietnamTime()
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
        }

        public async Task<IEnumerable<PromotionResponse>> GetAllPromotionsAsync(bool? isActiveOnly)
        {
            var now = GetVietnamTime();
            var promotions = await _promotionRepo.GetAllAsync(isActiveOnly);

            return promotions.Select(p => new PromotionResponse
            {
                PromotionId = p.PromotionId,
                Title = p.Title,
                Description = p.Description,
                DiscountPercent = p.DiscountPercent,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                Status = p.EndDate < now ? "Expired" : (p.StartDate > now ? "NotStarted" : "Active")
            });
        }

        public async Task<PromotionResponse> GetPromotionByIdAsync(Guid id)
        {
            var p = await _promotionRepo.GetByIdAsync(id);
            if (p == null) throw new Exception("Không tìm thấy chương trình khuyến mãi.");

            var now = GetVietnamTime();
            return new PromotionResponse
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

        public async Task<PromotionResponse> CreatePromotionAsync(CreatePromotionRequest request)
        {
            if (request.StartDate >= request.EndDate)
                throw new Exception("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

            var now = GetVietnamTime();

            // Khởi tạo thực thể cấu trúc chuẩn Entity
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

            return new PromotionResponse
            {
                PromotionId = promotion.PromotionId,
                Title = promotion.Title,
                Description = promotion.Description,
                DiscountPercent = promotion.DiscountPercent,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive,
                Status = promotion.StartDate > now ? "NotStarted" : "Active"
            };
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
            var now = GetVietnamTime();
            var promo = await _promotionRepo.GetByIdAsync(promotionId);

            // Kiểm tra tính hợp lệ của mã: Phải tồn tại, đang bật kích hoạt, và nằm trong khung thời gian cho phép
            if (promo == null || !promo.IsActive || promo.StartDate > now || promo.EndDate < now)
            {
                throw new Exception("Chương trình khuyến mãi không khả dụng hoặc đã hết hạn.");
            }

            if (promo.DiscountPercent.HasValue)
            {
                // Công thức tính số tiền được giảm: Giá gốc * (Phần trăm giảm / 100)
                return originalPrice * ((decimal)promo.DiscountPercent.Value / 100);
            }

            return 0;
        }
    }
}