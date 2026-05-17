using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly FlexFitDbContext _context; // Bổ sung DbContext để quản lý phân quyền
        public UserController(IUserRepository userRepo, FlexFitDbContext context)
        {
            _userRepo = userRepo;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userRepo.GetAllAsync();
            var dtos = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                DateOfBirth = u.DateOfBirth, // Bổ sung map trường này
                IsEmailVerified = u.IsEmailVerified,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            return Ok(new UserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                DateOfBirth = u.DateOfBirth, // Bổ sung map trường này
                IsEmailVerified = u.IsEmailVerified,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            // Chỉ cập nhật thông tin cá nhân (Bổ sung DateOfBirth)
            user.FullName = request.FullName ?? user.FullName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth; // Nhận ngày sinh mới
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);

            // Xóa dòng _userRepo.SaveChangesAsync() nếu trong Repository (UpdateAsync) của bạn đã gọi _db.SaveChangesAsync() rồi để tránh lỗi.
            // Nếu Repo của bạn chưa gọi SaveChanges thì giữ nguyên dòng dưới:
            // await _userRepo.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin cá nhân thành công!" });
        }

        // ==========================================
        // API QUYỀN LỰC: BẬT / TẮT TRẠNG THÁI (KHÓA TÀI KHOẢN)
        // ==========================================
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeUserStatus(Guid id, [FromBody] bool isActive)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);
            // Tương tự, nếu UpdateAsync đã save, bạn có thể bỏ dòng này
            // await _userRepo.SaveChangesAsync();

            string statusMessage = isActive ? "Mở khóa" : "Khóa";
            return Ok(new { message = $"{statusMessage} tài khoản thành công!" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            await _userRepo.DeleteAsync(id);
            // Tương tự, nếu DeleteAsync đã save, bạn có thể bỏ dòng này
            // await _userRepo.SaveChangesAsync();
            return Ok(new { message = "Xóa người dùng thành công!" });
        }
        [HttpPost("assign-role")]
        // [Authorize(Roles = "Admin")] // Mở comment dòng này ra nếu bạn đã cài đặt JWT Token
        public async Task<IActionResult> AssignRole([FromBody] UserRoleRequestDto request)
        {
            // 1. Kiểm tra người dùng có tồn tại không
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            // 2. Kiểm tra tên quyền (RoleName) có tồn tại trong hệ thống không
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.RoleName);
            if (role == null) return NotFound(new { message = $"Quyền '{request.RoleName}' không tồn tại trong hệ thống." });

            // 3. Kiểm tra xem người dùng đã có quyền này chưa
            var hasRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.RoleId);

            if (hasRole)
            {
                return Ok(new { message = $"Người dùng {user.FullName} hiện đã có quyền {request.RoleName} rồi." });
            }

            // 4. Cấp quyền mới
            var newUserRole = new UserRole
            {
                UserId = request.UserId,
                RoleId = role.RoleId,
                AssignedAt = DateTime.UtcNow
            };

            await _context.UserRoles.AddAsync(newUserRole);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã cấp quyền '{request.RoleName}' cho người dùng {user.FullName} thành công!" });
        }

        // ==========================================
        // DÀNH CHO ADMIN: THU HỒI QUYỀN CỦA NGƯỜI DÙNG
        // ==========================================
        [HttpDelete("revoke-role")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevokeRole([FromQuery] Guid userId, [FromQuery] string roleName)
        {
            // 1. Tìm User và Role
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return NotFound(new { message = $"Quyền '{roleName}' không tồn tại." });

            // 2. Tìm bản ghi cấp quyền trong bảng UserRoles
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.RoleId);

            if (userRole == null)
            {
                return Ok(new { message = $"Người dùng {user.FullName} hiện không sở hữu quyền '{roleName}' để thu hồi." });
            }

            // 3. Xóa quyền
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã thu hồi quyền '{roleName}' của người dùng {user.FullName} thành công!" });
        }
    }
}