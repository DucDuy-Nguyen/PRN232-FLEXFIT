using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexfit.Models;

namespace Flexfit.Repositories
{
    public interface IClassRepository
    {
        Task<IEnumerable<Class>> GetAllAsync();
        Task<IEnumerable<Class>> GetByBranchIdAsync(Guid branchId);
        Task<Class?> GetByIdAsync(Guid id);
        Task AddAsync(Class entity);
        Task UpdateAsync(Class entity);
        Task DeleteAsync(Guid id);
        Task<bool> BranchExistsAsync(Guid branchId);
        Task<bool> CategoryExistsAsync(Guid categoryId);
        Task<bool> CheckBranchOwnershipAsync(Guid branchId, Guid userId);
        Task<bool> CheckClassOwnershipAsync(Guid classId, Guid userId);
    }
}
