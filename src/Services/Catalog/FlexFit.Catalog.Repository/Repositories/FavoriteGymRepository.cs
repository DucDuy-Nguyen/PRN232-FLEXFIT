using FlexFit.Catalog.Repository.Data;
using FlexFit.Catalog.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Repository.Repositories;

public class FavoriteGymRepository : IFavoriteGymRepository
{
    private readonly CatalogDbContext _context;

    public FavoriteGymRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<FavoriteGym?> GetAsync(Guid userId, Guid gymId)
    {
        return await _context.FavoriteGyms
            .FirstOrDefaultAsync(f => f.UserId == userId && f.GymId == gymId);
    }

    public async Task<IEnumerable<FavoriteGym>> GetByUserIdAsync(Guid userId)
    {
        return await _context.FavoriteGyms
            .Include(f => f.Gym)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(FavoriteGym favoriteGym)
    {
        await _context.FavoriteGyms.AddAsync(favoriteGym);
    }

    public void Remove(FavoriteGym favoriteGym)
    {
        _context.FavoriteGyms.Remove(favoriteGym);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


