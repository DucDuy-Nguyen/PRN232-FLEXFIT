using Flexfit.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly FlexFitDbContext _db;
        public BranchRepository(FlexFitDbContext db) => _db = db;

        // Lấy tiện ích theo Id
        public async Task<GymAmenity?> GetAmenityByIdAsync(Guid amenityId) =>
            await _db.GymAmenities.FindAsync(amenityId);

        // Lấy chi nhánh theo Id kèm các mối quan hệ liên quan
        public async Task<Branch?> GetByIdAsync(Guid id) =>
            await _db.Branches
                .Include(b => b.Amenities)
                .Include(b => b.BranchImages)
                .Include(b => b.BranchStaffs)
                    .ThenInclude(bs => bs.Staff)
                .FirstOrDefaultAsync(b => b.BranchId == id);

        // Lấy danh sách chi nhánh theo OwnerId
        public async Task<IEnumerable<Branch>> GetByOwnerIdAsync(Guid ownerId) =>
            await _db.Branches
                .Include(b => b.Amenities)
                .Include(b => b.BranchImages)
                .Include(b => b.Gym)
                .Include(b => b.BranchStaffs)
                    .ThenInclude(bs => bs.Staff)
                .Where(b => b.Gym.OwnerId == ownerId)
                .ToListAsync();

        public async Task<IEnumerable<Branch>> GetAllAsync() =>
            await _db.Branches
                .Include(b => b.Amenities)
                .Include(b => b.BranchImages)
                .Include(b => b.BranchStaffs)
                    .ThenInclude(bs => bs.Staff)
                .ToListAsync();

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

        // 🛠️ ĐÃ SỬA LỖI: Dọn dẹp dữ liệu liên quan trước khi xóa nhánh gốc để tránh lỗi khóa ngoại (FK Constraint)
        public async Task DeleteAsync(Guid id)
        {
            // 1. Lấy chi nhánh lên và Include bảng trung gian Amenities
            var branch = await _db.Branches
                .Include(b => b.Amenities)
                .FirstOrDefaultAsync(b => b.BranchId == id);

            if (branch == null) return;

            // 2. Gỡ bỏ liên kết với Amenities
            if (branch.Amenities != null)
            {
                branch.Amenities.Clear();
            }

            // 3. Xóa toàn bộ Hình ảnh (Images)
            var branchImages = await _db.BranchImages.Where(i => i.BranchId == id).ToListAsync();
            if (branchImages.Any())
            {
                _db.BranchImages.RemoveRange(branchImages);
            }

            // 4. Xóa toàn bộ Nhân viên (Staffs)
            var branchStaffs = await _db.BranchStaffs.Where(s => s.BranchId == id).ToListAsync();
            if (branchStaffs.Any())
            {
                _db.BranchStaffs.RemoveRange(branchStaffs);
            }

            // 5. Lấy danh sách các Phiên tập (Sessions) thuộc chi nhánh
            var gymSessions = await _db.GymSessions.Where(gs => gs.BranchId == id).ToListAsync();
            if (gymSessions.Any())
            {
                // Lấy danh sách ID của các Session này
                var sessionIds = gymSessions.Select(gs => gs.SessionId).ToList();

                // 🚀 FIX LỖI MỚI NHẤT: Xóa toàn bộ Lịch đặt (Bookings) đang tham chiếu đến các Session này
                var gymBookings = await _db.GymBookings.Where(gb => sessionIds.Contains(gb.SessionId)).ToListAsync();
                if (gymBookings.Any())
                {
                    _db.GymBookings.RemoveRange(gymBookings);
                }

                // 6. Xóa các GymSessions sau khi đã dọn dẹp xong Bookings
                _db.GymSessions.RemoveRange(gymSessions);
            }

            // 7. Cuối cùng, xóa chi nhánh gốc
            _db.Branches.Remove(branch);

            // 8. Lưu toàn bộ thay đổi xuống Database
            await _db.SaveChangesAsync();
        }

        // Xóa ảnh cũ trực tiếp từ DbContext để tránh xung đột Tracking
        public async Task RemoveImagesByBranchIdAsync(Guid branchId)
        {
            var existingImages = _db.BranchImages.Where(img => img.BranchId == branchId);
            if (await existingImages.AnyAsync())
            {
                _db.BranchImages.RemoveRange(existingImages);
            }
        }

        // --- CÁC HÀM BỔ SUNG CHO LOGIC NHÂN SỰ ---
        public async Task<User?> GetUserByIdAsync(Guid userId) => await _db.Users.FindAsync(userId);

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.BranchStaffs)
                    .ThenInclude(bs => bs.Branch)
                        .ThenInclude(b => b.Gym)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        }

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
            return await _db.Gyms.AnyAsync(g => g.GymId == gymId && g.OwnerId == userId);
        }

        public async Task<bool> CheckBranchOwnershipAsync(Guid branchId, Guid userId)
        {
            return await _db.Branches
                .Include(b => b.Gym)
                .AnyAsync(b => b.BranchId == branchId && b.Gym.OwnerId == userId);
        }

        public async Task<IEnumerable<GymAmenity>> GetAllAmenitiesAsync() => await _db.GymAmenities.ToListAsync();

        public async Task AddAmenityAsync(GymAmenity amenity)
        {
            await _db.GymAmenities.AddAsync(amenity);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> AmenityExistsAsync(string amenityName) =>
            await _db.GymAmenities.AnyAsync(a => a.AmenityName.ToLower() == amenityName.Trim().ToLower());
        public async Task UpdateAmenityAsync(GymAmenity amenity)
        {
            _db.GymAmenities.Update(amenity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAmenityAsync(GymAmenity amenity)
        {
            // Lưu ý: Nếu tiện ích này đang được liên kết với nhiều Chi nhánh (Branch),
            // Entity Framework sẽ tự động xóa các dòng liên kết trong bảng trung gian 
            // nếu bạn đã cấu hình Cascade Delete trong DbContext.
            _db.GymAmenities.Remove(amenity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateBranchImagesDbAsync(Guid branchId, List<BranchImage> newImages)
        {
            // 1. Tìm tất cả ảnh cũ của chi nhánh này và XÓA CHÍNH XÁC khỏi Database
            var oldImages = await _db.BranchImages.Where(img => img.BranchId == branchId).ToListAsync();
            if (oldImages.Any())
            {
                _db.BranchImages.RemoveRange(oldImages);
            }

            // 2. Thêm toàn bộ ảnh mới vào
            if (newImages != null && newImages.Any())
            {
                await _db.BranchImages.AddRangeAsync(newImages);
            }

            // 3. Lưu xuống Database một lần duy nhất. EF Core sẽ không bị lỗi Tracking nữa!
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}