using Flexfit.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public interface IFavoriteGymRepository
    {
        Task<FavoriteGym?> GetAsync(Guid userId, Guid gymId);
        Task<IEnumerable<FavoriteGym>> GetByUserIdAsync(Guid userId);
        Task AddAsync(FavoriteGym favoriteGym);
        void Remove(FavoriteGym favoriteGym);
        Task SaveChangesAsync();
    }
}