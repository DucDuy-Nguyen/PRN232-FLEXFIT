using Flexfit.Models;

namespace Flexfit.Repositories
{
    public interface IBranchRepository
    {
        Task<IEnumerable<Branch>> GetAllAsync();
        Task<IEnumerable<Branch>> GetByOwnerIdAsync(Guid ownerId);
        Task<Branch?> GetByIdAsync(Guid id);
        Task AddAsync(Branch branch);
        Task UpdateAsync(Branch branch);
        Task DeleteAsync(Guid id);

        // --- HÀM CHO LOGIC HÌNH ẢNH (MỚI THÊM VÀO) ---
        Task RemoveImagesByBranchIdAsync(Guid branchId);

        // --- CÁC HÀM BỔ SUNG CHO LOGIC NHÂN SỰ & TIỆN ÍCH ---
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);

        Task<Role?> GetRoleByNameAsync(string roleName);
        Task<bool> UserHasRoleAsync(Guid userId, Guid roleId);
        Task AddUserRoleAsync(UserRole userRole);
        Task RemoveAllRolesOfUserAsync(Guid userId);
        Task RemoveUserRoleAsync(Guid userId, Guid roleId);

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

        Task SaveChangesAsync();
    }
}