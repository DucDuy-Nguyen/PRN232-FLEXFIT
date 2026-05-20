using Flexfit.DTOs;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;

namespace Flexfit.Services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepo;

        public BranchService(IBranchRepository branchRepo)
        {
            _branchRepo = branchRepo;
        }

        public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
        {
            var branches = await _branchRepo.GetAllAsync();
            return branches.Select(b => new BranchDto
            {
                BranchId = b.BranchId,
                GymId = b.GymId,
                BranchName = b.BranchName,
                Address = b.Address,
                City = b.City,
                District = b.District,
                OpenTime = b.OpenTime,
                CloseTime = b.CloseTime,
                ThumbnailUrl = b.ThumbnailUrl,
                CreditCost = b.CreditCost,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                Staffs = b.BranchStaffs.Select(bs => new StaffInfoDto
                {
                    StaffId = bs.StaffId,
                    FullName = bs.Staff?.FullName ?? "N/A"
                }).ToList()
            });
        }

        public async Task<BranchDto?> GetBranchByIdAsync(Guid id)
        {
            var b = await _branchRepo.GetByIdAsync(id);
            if (b == null) return null;

            return new BranchDto
            {
                BranchId = b.BranchId,
                GymId = b.GymId,
                BranchName = b.BranchName,
                Address = b.Address,
                City = b.City,
                District = b.District,
                OpenTime = b.OpenTime,
                CloseTime = b.CloseTime,
                ThumbnailUrl = b.ThumbnailUrl,
                CreditCost = b.CreditCost,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                Staffs = b.BranchStaffs.Select(bs => new StaffInfoDto
                {
                    StaffId = bs.StaffId,
                    FullName = bs.Staff?.FullName ?? "N/A"
                }).ToList()
            };
        }

        // ==========================================
        // KHU VỰC THAY ĐỔI DỮ LIỆU - BẮT BUỘC CHECK CHỦ PHÒNG
        // ==========================================

        public async Task<Guid> CreateBranchAsync(CreateBranchRequest request, Guid currentUserId)
        {
            //  CHECK: Người tạo phải là chủ của Gym này
            var isOwner = await _branchRepo.CheckGymOwnershipAsync(request.GymId, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không phải chủ của phòng gym này nên không thể tạo chi nhánh.");

            var newBranch = new Branch
            {
                BranchId = Guid.NewGuid(),
                GymId = request.GymId,
                BranchName = request.BranchName,
                Address = request.Address,
                City = request.City,
                District = request.District,
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                ThumbnailUrl = request.ThumbnailUrl,
                CreditCost = request.CreditCost,
                IsActive = true,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _branchRepo.AddAsync(newBranch);
            return newBranch.BranchId;
        }

        public async Task UpdateBranchAsync(Guid id, UpdateBranchRequest request, Guid currentUserId)
        {
            // 🛑 CHECK: Người sửa phải là chủ sở hữu của chi nhánh này
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(id, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            branch.BranchName = request.BranchName;
            branch.Address = request.Address;
            branch.City = request.City;
            branch.District = request.District;
            branch.OpenTime = request.OpenTime;
            branch.CloseTime = request.CloseTime;
            branch.ThumbnailUrl = request.ThumbnailUrl;
            branch.CreditCost = request.CreditCost;
            branch.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _branchRepo.UpdateAsync(branch);
        }

        public async Task ChangeBranchStatusAsync(Guid id, bool isActive, Guid currentUserId)
        {
            //  CHECK: Người đổi trạng thái phải là chủ sở hữu chi nhánh
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(id, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền thay đổi trạng thái chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            branch.IsActive = isActive;
            branch.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _branchRepo.UpdateAsync(branch);
        }

        public async Task DeleteBranchAsync(Guid id, Guid currentUserId)
        {
            //  CHECK: Người xóa phải là chủ sở hữu chi nhánh
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(id, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền xóa chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            await _branchRepo.DeleteAsync(id);
        }

        public async Task AssignStaffToBranchAsync(AssignStaffDto dto, Guid currentUserId)
        {
            // CHECK: Phải là chủ chi nhánh mới được bổ nhiệm nhân viên vào đây
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(dto.BranchId, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền quản lý nhân sự tại chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại trên hệ thống.");

            var employee = await _branchRepo.GetUserByIdAsync(dto.UserId);
            if (employee == null) throw new KeyNotFoundException("Người dùng được chọn làm nhân viên không tồn tại.");

            var staffRole = await _branchRepo.GetRoleByNameAsync("Staff");
            if (staffRole == null) throw new ArgumentException("Hệ thống chưa cấu hình vai trò 'Staff' trong DB!");

            var hasStaffRole = await _branchRepo.UserHasRoleAsync(dto.UserId, staffRole.RoleId);
            if (!hasStaffRole)
            {
                await _branchRepo.RemoveAllRolesOfUserAsync(dto.UserId);
                await _branchRepo.AddUserRoleAsync(new UserRole { UserId = dto.UserId, RoleId = staffRole.RoleId, AssignedAt = DateTimeHelper.GetVietnamTime() });
            }

            var isAlreadyStaffHere = await _branchRepo.IsStaffInBranchAsync(dto.UserId, dto.BranchId);
            if (isAlreadyStaffHere) throw new ArgumentException("Người này đã là nhân viên của chi nhánh này rồi!");

            await _branchRepo.RemoveStaffFromAllBranchesAsync(dto.UserId);
            await _branchRepo.AddBranchStaffAsync(new BranchStaff { StaffId = dto.UserId, BranchId = dto.BranchId, AssignedAt = DateTimeHelper.GetVietnamTime() });

            await _branchRepo.SaveChangesAsync();
        }

        public async Task RemoveStaffFromBranchAsync(Guid staffId, Guid branchId, Guid currentUserId)
        {
            //  CHECK: Phải là chủ chi nhánh mới được đuổi nhân viên
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(branchId, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền gỡ nhân sự tại chi nhánh này.");

            var branchStaff = await _branchRepo.GetBranchStaffAsync(staffId, branchId);
            if (branchStaff == null) throw new KeyNotFoundException("Nhân viên này hiện không thuộc chi nhánh này.");

            await _branchRepo.RemoveBranchStaffAsync(branchStaff);

            var remainingBranchesCount = await _branchRepo.CountBranchesForStaffAsync(staffId, branchId);
            if (remainingBranchesCount == 0)
            {
                var staffRole = await _branchRepo.GetRoleByNameAsync("Staff");
                if (staffRole != null)
                {
                    await _branchRepo.RemoveUserRoleAsync(staffId, staffRole.RoleId);
                }
            }
            await _branchRepo.SaveChangesAsync();
        }

        public async Task UpdateBranchStaffAsync(UpdateBranchStaffDto dto, Guid currentUserId)
        {
            //  CHECK: Phải là chủ chi nhánh mới được cập nhật nhân viên quản lý
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(dto.BranchId, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền chuyển giao nhân sự tại chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

            var newEmployee = await _branchRepo.GetUserByIdAsync(dto.NewStaffId);
            if (newEmployee == null) throw new KeyNotFoundException("Nhân viên mới được chọn không tồn tại.");

            var oldAssignment = await _branchRepo.GetBranchStaffByBranchIdAsync(dto.BranchId);
            if (oldAssignment != null)
            {
                Guid oldStaffId = oldAssignment.StaffId;
                if (oldStaffId == dto.NewStaffId) return;

                await _branchRepo.RemoveBranchStaffAsync(oldAssignment);

                var oldStaffRemainingCount = await _branchRepo.CountBranchesForStaffAsync(oldStaffId, dto.BranchId);
                if (oldStaffRemainingCount == 0)
                {
                    var staffRoleName = await _branchRepo.GetRoleByNameAsync("Staff");
                    if (staffRoleName != null)
                    {
                        await _branchRepo.RemoveUserRoleAsync(oldStaffId, staffRoleName.RoleId);
                    }
                }
            }

            var staffRole = await _branchRepo.GetRoleByNameAsync("Staff");
            if (staffRole == null) throw new ArgumentException("Hệ thống chưa cấu hình vai trò 'Staff' trong DB!");

            var hasStaffRole = await _branchRepo.UserHasRoleAsync(dto.NewStaffId, staffRole.RoleId);
            if (!hasStaffRole)
            {
                await _branchRepo.RemoveAllRolesOfUserAsync(dto.NewStaffId);
                await _branchRepo.AddUserRoleAsync(new UserRole { UserId = dto.NewStaffId, RoleId = staffRole.RoleId, AssignedAt = DateTimeHelper.GetVietnamTime() });
            }

            await _branchRepo.RemoveStaffFromAllBranchesAsync(dto.NewStaffId);
            await _branchRepo.AddBranchStaffAsync(new BranchStaff { StaffId = dto.NewStaffId, BranchId = dto.BranchId, AssignedAt = DateTimeHelper.GetVietnamTime() });

            await _branchRepo.SaveChangesAsync();
        }
    }
}