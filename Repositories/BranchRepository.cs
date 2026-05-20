using Flexfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly FlexFitDbContext _db;
        public BranchRepository(FlexFitDbContext db) => _db = db;

        public async Task<IEnumerable<Branch>> GetAllAsync() =>
            await _db.Branches
                .Include(b => b.BranchStaffs)
                    .ThenInclude(bs => bs.Staff)
                .ToListAsync();

        public async Task<Branch?> GetByIdAsync(Guid id) =>
            await _db.Branches
                .Include(b => b.BranchStaffs)
                    .ThenInclude(bs => bs.Staff)
                .FirstOrDefaultAsync(b => b.BranchId == id);

        public async Task AddAsync(Branch branch)
        {
            await _db.Branches.AddAsync(branch);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Branch branch)
        {
            _db.Branches.Update(branch);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch != null)
            {
                _db.Branches.Remove(branch);
                await _db.SaveChangesAsync();
            }
        }

        // --- TRIỂN KHAI CÁC HÀM BỔ SUNG ---
        public async Task<User?> GetUserByIdAsync(Guid userId) => await _db.Users.FindAsync(userId);

        public async Task<Role?> GetRoleByNameAsync(string roleName) => await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

        public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId) => await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        public async Task AddUserRoleAsync(UserRole userRole) => await _db.UserRoles.AddAsync(userRole);

        public async Task RemoveAllRolesOfUserAsync(Guid userId)
        {
            var roles = _db.UserRoles.Where(ur => ur.UserId == userId);
            _db.UserRoles.RemoveRange(roles);
        }

        public async Task RemoveUserRoleAsync(Guid userId, Guid roleId)
        {
            var userRole = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole != null) _db.UserRoles.Remove(userRole);
        }

        public async Task<bool> IsStaffInBranchAsync(Guid staffId, Guid branchId) => await _db.BranchStaffs.AnyAsync(bs => bs.StaffId == staffId && bs.BranchId == branchId);

        public async Task<BranchStaff?> GetBranchStaffAsync(Guid staffId, Guid branchId) => await _db.BranchStaffs.FirstOrDefaultAsync(bs => bs.StaffId == staffId && bs.BranchId == branchId);

        public async Task<BranchStaff?> GetBranchStaffByBranchIdAsync(Guid branchId) => await _db.BranchStaffs.FirstOrDefaultAsync(bs => bs.BranchId == branchId);

        public async Task AddBranchStaffAsync(BranchStaff branchStaff) => await _db.BranchStaffs.AddAsync(branchStaff);

        public async Task RemoveBranchStaffAsync(BranchStaff branchStaff)
        {
            _db.BranchStaffs.Remove(branchStaff);
            await Task.CompletedTask;
        }

        public async Task RemoveStaffFromAllBranchesAsync(Guid staffId)
        {
            var assignments = _db.BranchStaffs.Where(bs => bs.StaffId == staffId);
            _db.BranchStaffs.RemoveRange(assignments);
        }

        public async Task<int> CountBranchesForStaffAsync(Guid staffId, Guid excludeBranchId) => await _db.BranchStaffs.CountAsync(bs => bs.StaffId == staffId && bs.BranchId != excludeBranchId);
        public async Task<bool> CheckGymOwnershipAsync(Guid gymId, Guid userId)
        {
            // 💡 LƯU Ý: Thay 'OwnerId' bằng đúng tên trường lưu ID của chủ phòng trong bảng Gym của bạn (ví dụ: UserId, OwnerId,...)
            return await _db.Gyms.AnyAsync(g => g.GymId == gymId && g.OwnerId == userId);
        }

        public async Task<bool> CheckBranchOwnershipAsync(Guid branchId, Guid userId)
        {
            // Kiểm tra xem chi nhánh này có thuộc về phòng gym mà user này làm chủ không
            return await _db.Branches
                .Include(b => b.Gym)
                .AnyAsync(b => b.BranchId == branchId && b.Gym.OwnerId == userId);
        }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}