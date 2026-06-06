using Flexfit.DTOs;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Flexfit.Service; // Thêm namespace chứa INotificationService
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly INotificationService _notificationService; // 🔔 Thêm dịch vụ thông báo

        // Inject INotificationService vào Constructor
        public UserService(IUserRepository userRepo, INotificationService notificationService)
        {
            _userRepo = userRepo;
            _notificationService = notificationService;
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

        // =========================================================================
        // 1. GỬI THÔNG BÁO KHI TÀI KHOẢN BỊ KHÓA (BAN) HOẶC KÍCH HOẠT LẠI
        // =========================================================================
        public async Task ChangeUserStatusAsync(Guid id, bool isActive)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            user.IsActive = isActive;
            user.UpdatedAt = DateTimeHelper.GetVietnamTime();

            await _userRepo.UpdateAsync(user);

            // 🔔 Gửi thông báo hệ thống (Chạy fire-and-forget để không làm chậm API)
            string title = isActive ? "Tài khoản đã được kích hoạt" : "Tài khoản của bạn đã bị khóa";
            string content = isActive
                ? $"Chào {user.FullName}, tài khoản của bạn đã được quản trị viên kích hoạt lại. Bạn đã có thể sử dụng các dịch vụ của Flexfit."
                : $"Chào {user.FullName}, tài khoản của bạn đã bị tạm khóa bởi ban quản trị. Vui lòng liên hệ hotline bộ phận CSKH để biết thêm chi tiết.";

            _ = _notificationService.SendAsync(user.UserId, title, content, NotificationTypes.AccountUpdate);
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy người dùng.");

            await _userRepo.DeleteAsync(id);
        }

        // =========================================================================
        // 2. GỬI THÔNG BÁO KHI ĐƯỢC CẤP QUYỀN (ASSIGN ROLE)
        // =========================================================================
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

            // 🔔 Gửi thông báo cấp quyền mới thành công
            string title = "Cập nhật quyền hạn tài khoản";
            string content = $"Chúc mừng {user.FullName}, bạn đã được ban quản trị cấp thêm quyền hạn mới: [{request.RoleName}] vào hệ thống.";

            _ = _notificationService.SendAsync(user.UserId, title, content, NotificationTypes.AccountUpdate);

            return $"Đã cấp quyền '{request.RoleName}' cho người dùng {user.FullName} thành công!";
        }

        // =========================================================================
        // 3. GỬI THÔNG BÁO KHI BỊ THU HỒI QUYỀN (REVOKE ROLE)
        // =========================================================================
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

            // 🔔 Gửi thông báo thu hồi quyền thành công
            string title = "Thay đổi quyền hạn tài khoản";
            string content = $"Thông báo: Quyền hạn [{roleName}] của bạn đã bị ban quản trị thu hồi.";

            _ = _notificationService.SendAsync(user.UserId, title, content, NotificationTypes.AccountUpdate);

            return $"Đã thu hồi quyền '{roleName}' của người dùng {user.FullName} thành công!";
        }
    }
}