using FlexFit.CatalogService.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Repositories;

public interface IClassRepository
{
    Task<IEnumerable<Class>> GetAllAsync();
    Task<IEnumerable<Class>> GetByBranchIdAsync(Guid branchId);
    Task<IEnumerable<Class>> GetClassesByStaffIdAsync(Guid staffId);
    Task<IEnumerable<Class>> GetClassesByPartnerIdAsync(Guid partnerId);
    Task<Class?> GetByIdAsync(Guid id);
    Task AddAsync(Class entity);
    Task UpdateAsync(Class entity);
    Task DeleteAsync(Guid id);
    Task<bool> BranchExistsAsync(Guid branchId);
    Task<bool> CategoryExistsAsync(Guid categoryId);
    Task<bool> CheckBranchOwnershipAsync(Guid branchId, Guid userId);
    Task<bool> CheckClassOwnershipAsync(Guid classId, Guid userId);
    Task<(IEnumerable<Class> Items, int TotalCount)> GetClassesPagedAsync(string? search, Guid? branchId, Guid? categoryId, string? status, string? sortBy, string? sortDirection, int pageNumber, int pageSize);
}
