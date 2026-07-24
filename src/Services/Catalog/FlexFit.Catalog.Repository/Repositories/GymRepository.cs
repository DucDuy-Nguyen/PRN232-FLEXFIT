using FlexFit.Catalog.Repository.Data;
using FlexFit.Catalog.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Repository.Repositories;

public class GymRepository : IGymRepository
{
    private readonly CatalogDbContext _db;
    public GymRepository(CatalogDbContext db) => _db = db;

    public async Task<IEnumerable<Gym>> GetAllAsync()
    {
        return await _db.Gyms.ToListAsync();
    }

    public async Task<IEnumerable<Gym>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _db.Gyms.Where(g => g.OwnerId == ownerId).ToListAsync();
    }

    public async Task<Gym?> GetByIdAsync(Guid id) => await _db.Gyms.FindAsync(id);

    public async Task AddAsync(Gym gym)
    {
        await _db.Gyms.AddAsync(gym);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Gym gym)
    {
        _db.Gyms.Update(gym);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var gym = await _db.Gyms.FindAsync(id);
        if (gym != null)
        {
            _db.Gyms.Remove(gym);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> CheckGymOwnershipAsync(Guid gymId, Guid userId)
    {
        return await _db.Gyms.AnyAsync(g => g.GymId == gymId && g.OwnerId == userId);
    }

    public async Task<int> CountGymsByOwnerIdAsync(Guid ownerId)
    {
        return await _db.Gyms.CountAsync(g => g.OwnerId == ownerId);
    }

    public async Task<IEnumerable<Gym>> GetOwnedGymsExceptAsync(Guid ownerId, Guid excludedGymId)
    {
        return await _db.Gyms
            .Where(g => g.OwnerId == ownerId && g.GymId != excludedGymId)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Gym> Items, int TotalCount)> GetGymsPagedAsync(string? search, string? status, Guid? ownerId, string? sortBy, string? sortDirection, int pageNumber, int pageSize)
    {
        var query = _db.Gyms.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g => g.GymName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(g => g.Status == status);
        }

        if (ownerId.HasValue)
        {
            query = query.Where(g => g.OwnerId == ownerId.Value);
        }

        bool isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase))
        {
            query = isDescending ? query.OrderByDescending(g => g.GymName) : query.OrderBy(g => g.GymName);
        }
        else if (string.Equals(sortBy, "rating", StringComparison.OrdinalIgnoreCase))
        {
            query = isDescending ? query.OrderByDescending(g => g.RatingAverage) : query.OrderBy(g => g.RatingAverage);
        }
        else
        {
            query = query.OrderByDescending(g => g.CreatedAt);
        }

        int totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}


