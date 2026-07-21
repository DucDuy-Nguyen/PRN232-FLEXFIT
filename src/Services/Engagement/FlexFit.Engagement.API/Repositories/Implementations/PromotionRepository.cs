using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Repositories.Interfaces;
using FlexFit.Engagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FlexFit.Engagement.API.Repositories.Implementations;

public class PromotionRepository : IPromotionRepository
{
    private readonly EngagementDbContext _context;

    public PromotionRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Promotion>> GetAllAsync(bool? isActiveOnly)
    {
        var query = _context.Promotions.AsQueryable();
        if (isActiveOnly == true)
            query = query.Where(p => p.IsActive);
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Promotion?> GetByIdAsync(Guid id)
    {
        return await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
    }

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
