using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.EntityFrameworkCore;
using PayOS.Resources.V1.Payouts.Batch;

namespace Flexfit.Services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepo;
        private readonly FlexFitDbContext _context;

        public BranchService(IBranchRepository branchRepo, FlexFitDbContext context)
        {
            _branchRepo = branchRepo;
            _context = context;
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
                    FullName = bs.Staff.FullName
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
                    FullName = bs.Staff.FullName
                }).ToList()
            };
        }

        public async Task<Guid> CreateBranchAsync(CreateBranchRequest request)
        {
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
                CreatedAt = DateTime.UtcNow
            };

            await _branchRepo.AddAsync(newBranch);
            return newBranch.BranchId;
        }

        public async Task UpdateBranchAsync(Guid id, UpdateBranchRequest request)
        {
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
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepo.UpdateAsync(branch);
        }

        public async Task ChangeBranchStatusAsync(Guid id, bool isActive)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            branch.IsActive = isActive;
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepo.UpdateAsync(branch);
        }

        public async Task DeleteBranchAsync(Guid id)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            await _branchRepo.DeleteAsync(id);
        }

        public async Task AssignStaffToBranchAsync(AssignStaffDto dto)
        {
            var branch = await _context.Branches.FindAsync(dto.BranchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại trên hệ thống.");

            var employee = await _context.Users.FindAsync(dto.UserId);
            if (employee == null) throw new KeyNotFoundException("Người dùng được chọn làm nhân viên không tồn tại.");

            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
            if (staffRole == null) throw new ArgumentException("Hệ thống chưa cấu hình vai trò 'Staff' trong DB!");

            var hasStaffRole = await _context.UserRoles.AnyAsync(ur => ur.UserId == dto.UserId && ur.RoleId == staffRole.RoleId);
            if (!hasStaffRole)
            {
                var oldRoles = _context.UserRoles.Where(ur => ur.UserId == dto.UserId);
                _context.UserRoles.RemoveRange(oldRoles);

                await _context.UserRoles.AddAsync(new UserRole { UserId = dto.UserId, RoleId = staffRole.RoleId, AssignedAt = DateTime.UtcNow });
            }

            var isAlreadyStaffHere = await _context.BranchStaffs.AnyAsync(bs => bs.StaffId == dto.UserId && bs.BranchId == dto.BranchId);
            if (isAlreadyStaffHere) throw new ArgumentException("Người này đã là nhân viên của chi nhánh này rồi!");

            var oldBranchAssignments = _context.BranchStaffs.Where(bs => bs.StaffId == dto.UserId);
            _context.BranchStaffs.RemoveRange(oldBranchAssignments);

            await _context.BranchStaffs.AddAsync(new BranchStaff { StaffId = dto.UserId, BranchId = dto.BranchId, AssignedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveStaffFromBranchAsync(Guid staffId, Guid branchId)
        {
            var branchStaff = await _context.BranchStaffs.FirstOrDefaultAsync(bs => bs.StaffId == staffId && bs.BranchId == branchId);
            if (branchStaff == null) throw new KeyNotFoundException("Nhân viên này hiện không thuộc chi nhánh này hoặc không tồn tại bản ghi bổ nhiệm.");

            _context.BranchStaffs.Remove(branchStaff);

            var remainingBranchesCount = await _context.BranchStaffs.CountAsync(bs => bs.StaffId == staffId && bs.BranchId != branchId);
            if (remainingBranchesCount == 0)
            {
                var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
                if (staffRole != null)
                {
                    var userStaffRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == staffId && ur.RoleId == staffRole.RoleId);
                    if (userStaffRole != null) _context.UserRoles.Remove(userStaffRole);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBranchStaffAsync(UpdateBranchStaffDto dto)
        {
            var branch = await _context.Branches.FindAsync(dto.BranchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

            var newEmployee = await _context.Users.FindAsync(dto.NewStaffId);
            if (newEmployee == null) throw new KeyNotFoundException("Nhân viên mới được chọn không tồn tại.");

            var oldAssignment = await _context.BranchStaffs.FirstOrDefaultAsync(bs => bs.BranchId == dto.BranchId);
            if (oldAssignment != null)
            {
                Guid oldStaffId = oldAssignment.StaffId;
                if (oldStaffId == dto.NewStaffId) return; // Đã là nhân viên hiện tại, bỏ qua không báo lỗi

                _context.BranchStaffs.Remove(oldAssignment);

                var oldStaffRemainingCount = await _context.BranchStaffs.CountAsync(bs => bs.StaffId == oldStaffId && bs.BranchId != dto.BranchId);
                if (oldStaffRemainingCount == 0)
                {
                    var staffRoleName = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
                    if (staffRoleName != null)
                    {
                        var oldUserStaffRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == oldStaffId && ur.RoleId == staffRoleName.RoleId);
                        if (oldUserStaffRole != null) _context.UserRoles.Remove(oldUserStaffRole);
                    }
                }
            }

            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
            if (staffRole == null) throw new ArgumentException("Hệ thống chưa cấu hình vai trò 'Staff' trong DB!");

            var hasStaffRole = await _context.UserRoles.AnyAsync(ur => ur.UserId == dto.NewStaffId && ur.RoleId == staffRole.RoleId);
            if (!hasStaffRole)
            {
                var oldRolesOfNewStaff = _context.UserRoles.Where(ur => ur.UserId == dto.NewStaffId);
                _context.UserRoles.RemoveRange(oldRolesOfNewStaff);

                await _context.UserRoles.AddAsync(new UserRole { UserId = dto.NewStaffId, RoleId = staffRole.RoleId, AssignedAt = DateTime.UtcNow });
            }

            var oldBranchAssignmentsOfNewStaff = _context.BranchStaffs.Where(bs => bs.StaffId == dto.NewStaffId);
            _context.BranchStaffs.RemoveRange(oldBranchAssignmentsOfNewStaff);

            await _context.BranchStaffs.AddAsync(new BranchStaff { StaffId = dto.NewStaffId, BranchId = dto.BranchId, AssignedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
        }
    }
}