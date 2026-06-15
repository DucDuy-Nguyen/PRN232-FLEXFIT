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
        Task<IEnumerable<ClassDto>> GetClassesByStaffIdAsync(Guid staffId);
        Task<IEnumerable<ClassDto>> GetClassesByPartnerIdAsync(Guid partnerId);
        Task<ClassDto?> GetClassByIdAsync(Guid id);
        Task<Guid> CreateClassAsync(CreateClassRequest request, Guid currentUserId);
        Task UpdateClassAsync(Guid id, UpdateClassRequest request, Guid currentUserId);
        Task ChangeClassStatusAsync(Guid id, string status, Guid currentUserId);
        Task DeleteClassAsync(Guid id, Guid currentUserId);
    }
}
