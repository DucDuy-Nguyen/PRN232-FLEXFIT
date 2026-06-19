using Flexfit.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Repository
{
    public interface IPromotionRepository
    {
        Task<IEnumerable<Promotion>> GetAllAsync(bool? isActiveOnly);
        Task<Promotion?> GetByIdAsync(Guid id);
        Task<Promotion?> GetBestActivePromotionAsync(DateTime now);
        Task AddAsync(Promotion promotion);
        Task DeleteAsync(Promotion promotion);
        Task SaveChangesAsync();
    }
}
