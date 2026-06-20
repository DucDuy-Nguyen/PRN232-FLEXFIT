using Flexfit.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flexfit.Services
{
    public interface IBranchService
    {
        // 🔓 Hàm đọc dữ liệu công khai - Không cần check UserId
        Task<IEnumerable<BranchDto>> GetAllBranchesAsync();
        Task<IEnumerable<BranchDto>> GetBranchesByPartnerIdAsync(Guid ownerId);
        Task<BranchDto?> GetBranchByIdAsync(Guid id);

        // 🔐 Hàm thay đổi dữ liệu - Bắt buộc truyền currentUserId để kiểm tra quyền sở hữu
        Task<Guid> CreateBranchAsync(CreateBranchRequest request, Guid currentUserId);
        Task UpdateBranchAsync(Guid id, UpdateBranchRequest request, Guid currentUserId);
        Task ChangeBranchStatusAsync(Guid id, bool isActive, Guid currentUserId);
        Task DeleteBranchAsync(Guid id, Guid currentUserId);

        // 👥 Hàm quản lý nhân sự chi nhánh - Kiểm tra quyền sở hữu chi nhánh
        Task AssignStaffToBranchAsync(AssignStaffDto dto, Guid currentUserId);
        Task AssignStaffToBranchByEmailAsync(AssignStaffByEmailDto dto, Guid currentUserId);

        Task RemoveStaffFromBranchAsync(Guid staffId, Guid branchId, Guid currentUserId);
        Task UpdateBranchStaffAsync(UpdateBranchStaffDto dto, Guid currentUserId);
        // 🔐 Quản lý danh sách tiện ích của chi nhánh (Yêu cầu Check chủ sở hữu chi nhánh hoặc Staff thuộc chi nhánh đó)
        Task UpdateBranchAmenitiesAsync(Guid branchId, UpdateBranchAmenitiesRequest request, Guid currentUserId);
        Task<IEnumerable<GymAmenityDto>> GetAllAmenitiesAsync();
        Task<Guid> CreateAmenityAsync(string amenityName);
    }
}
