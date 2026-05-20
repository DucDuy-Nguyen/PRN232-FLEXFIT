using Flexfit.DTOs;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;

namespace Flexfit.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepo.GetAllAsync();
            return users.Select(u => new UserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                DateOfBirth = u.DateOfBirth,
                IsEmailVerified = u.IsEmailVerified,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null) return null;

            return new UserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                DateOfBirth = u.DateOfBirth,
                IsEmailVerified = u.IsEmailVerified,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            };
        }

        public async Task UpdateUserAsync(Guid id, UpdateUserRequest request)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            user.FullName = request.FullName ?? user.FullName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
            user.DateOfBirth = request.DateOfBirth ?? user.DateOfBirth;
            user.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _userRepo.UpdateAsync(user);
        }

        public async Task ChangeUserStatusAsync(Guid id, bool isActive)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            user.IsActive = isActive;
            user.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _userRepo.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            await _userRepo.DeleteAsync(id);
        }

        public async Task<string> AssignRoleAsync(UserRoleRequestDto request)
        {
            var user = await _userRepo.GetByIdAsync(request.UserId);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            var role = await _userRepo.GetRoleByNameAsync(request.RoleName);
            if (role == null) throw new ArgumentException($"Quyền '{request.RoleName}' không tồn tại trong hệ thống.");

            var hasRole = await _userRepo.GetUserRoleAsync(request.UserId, role.RoleId);
            if (hasRole != null)
            {
                return $"Người dùng {user.FullName} hiện đã có quyền {request.RoleName} rồi.";
            }

            var newUserRole = new UserRole
            {
                UserId = request.UserId,
                RoleId = role.RoleId,
                AssignedAt = DateTimeHelper.GetVietnamTime()
            };

            await _userRepo.AddUserRoleAsync(newUserRole);
            return $"Đã cấp quyền '{request.RoleName}' cho người dùng {user.FullName} thành công!";
        }

        public async Task<string> RevokeRoleAsync(Guid userId, string roleName)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            var role = await _userRepo.GetRoleByNameAsync(roleName);
            if (role == null) throw new ArgumentException($"Quyền '{roleName}' không tồn tại.");

            var userRole = await _userRepo.GetUserRoleAsync(userId, role.RoleId);
            if (userRole == null)
            {
                return $"Người dùng {user.FullName} hiện không sở hữu quyền '{roleName}' để thu hồi.";
            }

            await _userRepo.RemoveUserRoleAsync(userRole);
            return $"Đã thu hồi quyền '{roleName}' của người dùng {user.FullName} thành công!";
        }
    }
}