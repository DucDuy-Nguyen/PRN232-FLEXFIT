using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UserController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
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
    }
}