using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexfit.DTOs;

namespace Flexfit.Services
{
    public interface IClassService
    {
        Task<IEnumerable<ClassDto>> GetAllClassesAsync();
        Task<IEnumerable<ClassDto>> GetClassesByBranchAsync(Guid branchId);
        Task<ClassDto?> GetClassByIdAsync(Guid id);
        Task<Guid> CreateClassAsync(CreateClassRequest request);
        Task UpdateClassAsync(Guid id, UpdateClassRequest request);
        Task ChangeClassStatusAsync(Guid id, string status);
        Task DeleteClassAsync(Guid id);
    }
}
