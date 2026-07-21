using FlexFit.Catalog.Repository.Data;
using FlexFit.Catalog.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Repository.Repositories;

public class ClassRepository : IClassRepository
{
    private readonly CatalogDbContext _db;

    public ClassRepository(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Class>> GetAllAsync()
    {
        return await _db.Classes
            .Include(c => c.Branch)
            .Include(c => c.Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<Class>> GetByBranchIdAsync(Guid branchId)
    {
        return await _db.Classes
            .Include(c => c.Branch)
            .Include(c => c.Category)
            .Where(c => c.BranchId == branchId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Class>> GetClassesByStaffIdAsync(Guid staffId)
    {
        return await _db.Classes
            .Include(c => c.Branch)
            .Include(c => c.Category)
            .Where(c => c.Branch.BranchStaffs.Any(bs => bs.StaffId == staffId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Class>> GetClassesByPartnerIdAsync(Guid partnerId)
    {
        return await _db.Classes
            .Include(c => c.Branch)
                .ThenInclude(b => b.Gym)
            .Include(c => c.Category)
            .Where(c => c.Branch.Gym.OwnerId == partnerId)
            .ToListAsync();
    }

    public async Task<Class?> GetByIdAsync(Guid id)
    {
        return await _db.Classes
            .Include(c => c.Branch)
            .Include(c => c.Category)
            .Include(c => c.ClassSchedules)
            .FirstOrDefaultAsync(c => c.ClassId == id);
    }

    public async Task AddAsync(Class entity)
    {
        await _db.Classes.AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Class entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.Classes.FindAsync(id);
        if (entity != null)
        {
            _db.Classes.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> BranchExistsAsync(Guid branchId)
    {
        return await _db.Branches.AnyAsync(b => b.BranchId == branchId);
    }

    public async Task<bool> CategoryExistsAsync(Guid categoryId)
    {
        return await _db.Categories.AnyAsync(c => c.CategoryId == categoryId);
    }

    public async Task<bool> CheckBranchOwnershipAsync(Guid branchId, Guid userId)
    {
        return await _db.Branches
            .Include(b => b.Gym)
            .AnyAsync(b => b.BranchId == branchId && b.Gym.OwnerId == userId);
    }

    public async Task<bool> CheckClassOwnershipAsync(Guid classId, Guid userId)
    {
        return await _db.Classes
            .Include(c => c.Branch)
                .ThenInclude(b => b.Gym)
            .AnyAsync(c => c.ClassId == classId && c.Branch.Gym.OwnerId == userId);
    }

    public async Task<(IEnumerable<Class> Items, int TotalCount)> GetClassesPagedAsync(string? search, Guid? branchId, Guid? categoryId, string? status, string? sortBy, string? sortDirection, int pageNumber, int pageSize)
    {
        var query = _db.Classes
            .Include(c => c.Branch)
            .Include(c => c.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.ClassName.Contains(search));
        }

        if (branchId.HasValue)
        {
            query = query.Where(c => c.BranchId == branchId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        bool isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase))
        {
            query = isDescending ? query.OrderByDescending(c => c.ClassName) : query.OrderBy(c => c.ClassName);
        }
        else if (string.Equals(sortBy, "credit", StringComparison.OrdinalIgnoreCase))
        {
            query = isDescending ? query.OrderByDescending(c => c.CreditCost) : query.OrderBy(c => c.CreditCost);
        }
        else if (string.Equals(sortBy, "time", StringComparison.OrdinalIgnoreCase))
        {
            query = isDescending ? query.OrderByDescending(c => c.StartTime) : query.OrderBy(c => c.StartTime);
        }
        else
        {
            query = query.OrderByDescending(c => c.CreatedAt);
        }

        int totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, totalCount);
    }
}


