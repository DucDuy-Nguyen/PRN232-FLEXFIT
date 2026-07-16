using FlexFit.CatalogService.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Service;

public interface IBranchService
{
    Task<IEnumerable<BranchDto>> GetAllBranchesAsync();
    Task<IEnumerable<BranchDto>> GetBranchesByPartnerIdAsync(Guid ownerId);
    Task<BranchDto?> GetBranchByIdAsync(Guid id);
    Task<Guid> CreateBranchAsync(CreateBranchRequest request, Guid currentUserId);
    Task UpdateBranchAsync(Guid id, UpdateBranchRequest request, Guid currentUserId);
    Task ChangeBranchStatusAsync(Guid id, bool isActive, Guid currentUserId);
    Task DeleteBranchAsync(Guid id, Guid currentUserId);
    Task AssignStaffToBranchAsync(AssignStaffDto dto, Guid currentUserId);
    Task AssignStaffToBranchByEmailAsync(AssignStaffByEmailDto dto, Guid currentUserId);
    Task RemoveStaffFromBranchAsync(Guid staffId, Guid branchId, Guid currentUserId);
    Task UpdateBranchStaffAsync(UpdateBranchStaffDto dto, Guid currentUserId);
    Task UpdateBranchAmenitiesAsync(Guid branchId, UpdateBranchAmenitiesRequest request, Guid currentUserId);
    Task<IEnumerable<GymAmenityDto>> GetAllAmenitiesAsync();
    Task<Guid> CreateAmenityAsync(string amenityName);
    Task UpdateBranchImagesAsync(Guid branchId, UpdateBranchImagesRequest request, Guid currentUserId);
    Task UpdateAmenityAsync(Guid amenityId, string newAmenityName);
    Task DeleteAmenityAsync(Guid amenityId);
}
