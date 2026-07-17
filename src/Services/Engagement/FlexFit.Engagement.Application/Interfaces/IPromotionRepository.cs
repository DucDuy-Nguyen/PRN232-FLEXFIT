using FlexFit.Engagement.Domain.Entities;

namespace FlexFit.Engagement.Application.Interfaces;

public interface IPromotionRepository
{
    Task<IEnumerable<Promotion>> GetAllAsync(bool? isActiveOnly);
    Task<Promotion?> GetByIdAsync(Guid id);
    Task AddAsync(Promotion promotion);
    Task DeleteAsync(Promotion promotion);
    Task SaveChangesAsync();
}
