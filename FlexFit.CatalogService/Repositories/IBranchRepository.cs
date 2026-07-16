using FlexFit.CatalogService.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Repositories;

public interface IBranchRepository
{
    Task<IEnumerable<Branch>> GetAllAsync();
    Task<IEnumerable<Branch>> GetByOwnerIdAsync(Guid ownerId);
    Task<Branch?> GetByIdAsync(Guid id);
    Task AddAsync(Branch branch);
    Task UpdateAsync(Branch branch);
    Task DeleteAsync(Guid id);
    Task RemoveImagesByBranchIdAsync(Guid branchId);
    Task<bool> IsStaffInBranchAsync(Guid staffId, Guid branchId);
    Task<BranchStaff?> GetBranchStaffAsync(Guid staffId, Guid branchId);
    Task<BranchStaff?> GetBranchStaffByBranchIdAsync(Guid branchId);
    Task AddBranchStaffAsync(BranchStaff branchStaff);
    Task RemoveBranchStaffAsync(BranchStaff branchStaff);
    Task RemoveStaffFromAllBranchesAsync(Guid staffId);
    Task<int> CountBranchesForStaffAsync(Guid staffId, Guid excludeBranchId);
    Task<bool> CheckGymOwnershipAsync(Guid gymId, Guid userId);
    Task<bool> CheckBranchOwnershipAsync(Guid branchId, Guid userId);
    Task<GymAmenity?> GetAmenityByIdAsync(Guid amenityId);
    Task<IEnumerable<GymAmenity>> GetAllAmenitiesAsync();
    Task AddAmenityAsync(GymAmenity amenity);
    Task<bool> AmenityExistsAsync(string amenityName);
    Task UpdateBranchImagesDbAsync(Guid branchId, List<BranchImage> newImages);
    Task UpdateAmenityAsync(GymAmenity amenity);
    Task DeleteAmenityAsync(GymAmenity amenity);
    Task SaveChangesAsync();
}
