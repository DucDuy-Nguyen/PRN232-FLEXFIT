using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Repository
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly FlexFitDbContext _context;

        public PromotionRepository(FlexFitDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Promotion>> GetAllAsync(bool? isActiveOnly)
        {
            // Lấy thời gian hiện tại theo múi giờ Việt Nam (GMT+7)
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            var query = _context.Promotions.AsQueryable();

            if (isActiveOnly == true)
            {
                query = query.Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now);
            }

            return await query.ToListAsync();
        }

        public async Task<Promotion?> GetByIdAsync(Guid id)
        {
            return await _context.Promotions.FindAsync(id);
        }

        // XÓA BỎ HÀM GetByCodeAsync vì hệ thống không dùng PromoCode

        public async Task AddAsync(Promotion promotion)
        {
            await _context.Promotions.AddAsync(promotion);
        }

        public async Task DeleteAsync(Promotion promotion)
        {
            _context.Promotions.Remove(promotion);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}