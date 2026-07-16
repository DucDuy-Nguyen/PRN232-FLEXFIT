using FlexFit.CatalogService.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Service;

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
    Task<PaginatedList<ClassDto>> GetClassesPagedAsync(string? search, Guid? branchId, Guid? categoryId, string? status, string? sortBy, string? sortDirection, int pageNumber, int pageSize);
}
