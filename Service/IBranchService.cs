using Flexfit.DTOs;

namespace Flexfit.Services
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchDto>> GetAllBranchesAsync();
        Task<BranchDto?> GetBranchByIdAsync(Guid id);
        Task<Guid> CreateBranchAsync(CreateBranchRequest request);
        Task UpdateBranchAsync(Guid id, UpdateBranchRequest request);
        Task ChangeBranchStatusAsync(Guid id, bool isActive);
        Task DeleteBranchAsync(Guid id);
        Task AssignStaffToBranchAsync(AssignStaffDto dto);
        Task RemoveStaffFromBranchAsync(Guid staffId, Guid branchId);
        Task UpdateBranchStaffAsync(UpdateBranchStaffDto dto);
    }
}