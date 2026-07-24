using FlexFit.Catalog.Repository.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Repository.Repositories;

public interface IFavoriteClassRepository
{
    Task<FavoriteClass?> GetAsync(Guid userId, Guid classId);
    Task<IEnumerable<FavoriteClass>> GetByUserIdAsync(Guid userId);
    Task AddAsync(FavoriteClass favoriteClass);
    void Remove(FavoriteClass favoriteClass);
    Task SaveChangesAsync();
}

