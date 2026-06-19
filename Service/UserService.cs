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
        private readonly IGymRepository _gymRepo;
        private readonly IBranchRepository _branchRepo;

        // Inject INotificationService vào Constructor
        public UserService(IUserRepository userRepo, INotificationService notificationService, IGymRepository gymRepo, IBranchRepository branchRepo)
        {
            _userRepo = userRepo;
            _notificationService = notificationService;
            _gymRepo = gymRepo;
            _branchRepo = branchRepo;
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
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles?.Select(ur => ur.Role.RoleName).ToList() ?? new List<string>(),
                AssignedGymName = u.Gyms?.FirstOrDefault()?.GymName,
                AssignedBranchName = u.BranchStaffs?.FirstOrDefault()?.Branch?.BranchName
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
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles?.Select(ur => ur.Role.RoleName).ToList() ?? new List<string>(),
                AssignedGymName = u.Gyms?.FirstOrDefault()?.GymName,
                AssignedBranchName = u.BranchStaffs?.FirstOrDefault()?.Branch?.BranchName
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

            var roleName = (request.Role ?? request.RoleName)?.Trim();
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("Vui lòng chọn vai trò.");
            request.RoleName = roleName;
            Console.WriteLine($"AssignRole user={request.UserId}, role={roleName}, branchId={request.BranchId}, gymId={request.GymId}");

            var role = await _userRepo.GetRoleByNameAsync(roleName);
            if (role == null) throw new ArgumentException($"Quyền '{request.RoleName}' không tồn tại trong hệ thống.");

            if (request.RoleName == "GymPartner" && !request.GymId.HasValue)
            {
                throw new ArgumentException("Vui lòng chọn phòng gym cho đối tác");
            }

            if (request.RoleName == "Staff" && !request.BranchId.HasValue)
            {
                throw new ArgumentException("Vui lòng chọn chi nhánh cho nhân viên");
            }

            if (request.RoleName == "GymPartner" && await _gymRepo.GetByIdAsync(request.GymId!.Value) == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phòng gym.");
            }

            if (request.RoleName == "Staff" && await _branchRepo.GetByIdAsync(request.BranchId!.Value) == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chi nhánh.");
            }

            if (request.RoleName == "Staff" && await _gymRepo.CountGymsByOwnerIdAsync(request.UserId) > 0)
            {
                throw new ArgumentException("User đang là chủ phòng gym. Vui lòng chuyển quyền sở hữu gym trước khi gán Staff.");
            }

            if (request.RoleName == "GymPartner")
            {
                var ownedOtherGyms = (await _gymRepo.GetOwnedGymsExceptAsync(request.UserId, request.GymId!.Value)).ToList();
                if (ownedOtherGyms.Count > 0)
                {
                    var gymNames = string.Join(", ", ownedOtherGyms.Select(g => g.GymName));
                    throw new ArgumentException($"User đang là owner của gym khác: {gymNames}. Vui lòng chuyển owner các gym đó trước khi gán gym mới.");
                }
            }

            if (request.RoleName == "Member" || request.RoleName == "Admin")
            {
                await _branchRepo.RemoveStaffFromAllBranchesAsync(request.UserId);
                await _branchRepo.SaveChangesAsync();
            }

            if (request.RoleName == "GymPartner")
            {
                var staffRole = await _userRepo.GetRoleByNameAsync("Staff");
                if (staffRole != null)
                {
                    var staffUserRole = await _userRepo.GetUserRoleAsync(request.UserId, staffRole.RoleId);
                    if (staffUserRole != null)
                    {
                        await _userRepo.RemoveUserRoleAsync(staffUserRole);
                    }
                }
            }
            else if (request.RoleName == "Staff")
            {
                var partnerRole = await _userRepo.GetRoleByNameAsync("GymPartner");
                if (partnerRole != null)
                {
                    var partnerUserRole = await _userRepo.GetUserRoleAsync(request.UserId, partnerRole.RoleId);
                    if (partnerUserRole != null)
                    {
                        await _userRepo.RemoveUserRoleAsync(partnerUserRole);
                    }
                }
            }

            var hasRole = await _userRepo.GetUserRoleAsync(request.UserId, role.RoleId);
            if (hasRole != null)
            {
                if (request.RoleName == "GymPartner")
                {
                    var gym = await _gymRepo.GetByIdAsync(request.GymId!.Value);
                    if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng gym.");

                    var oldOwnerId = gym.OwnerId;
                    gym.OwnerId = request.UserId;
                    await _gymRepo.UpdateAsync(gym);
                    await _branchRepo.RemoveStaffFromAllBranchesAsync(request.UserId);
                    await _branchRepo.SaveChangesAsync();

                    if (oldOwnerId != request.UserId && await _gymRepo.CountGymsByOwnerIdAsync(oldOwnerId) == 0)
                    {
                        await _gymRepo.RemoveUserRoleAsync(oldOwnerId, role.RoleId);
                        await _gymRepo.SaveChangesAsync();
                    }
                }
                else if (request.RoleName == "Staff")
                {
                    var branch = await _branchRepo.GetByIdAsync(request.BranchId!.Value);
                    if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

                    await _branchRepo.RemoveStaffFromAllBranchesAsync(request.UserId);
                    await _branchRepo.SaveChangesAsync();
                    await _branchRepo.AddBranchStaffAsync(new BranchStaff
                    {
                        StaffId = request.UserId,
                        BranchId = request.BranchId.Value,
                        AssignedAt = DateTimeHelper.GetVietnamTime()
                    });
                    await _branchRepo.SaveChangesAsync();
                }
                return $"Người dùng {user.FullName} hiện đã có quyền {request.RoleName} rồi.";
            }

            var newUserRole = new UserRole
            {
                UserId = request.UserId,
                RoleId = role.RoleId,
                AssignedAt = DateTimeHelper.GetVietnamTime()
            };

            await _userRepo.AddUserRoleAsync(newUserRole);

            if (request.RoleName == "GymPartner")
            {
                var gym = await _gymRepo.GetByIdAsync(request.GymId!.Value);
                if (gym == null) throw new KeyNotFoundException("Không tìm thấy phòng gym.");

                var oldOwnerId = gym.OwnerId;
                gym.OwnerId = request.UserId;
                await _gymRepo.UpdateAsync(gym);
                await _branchRepo.RemoveStaffFromAllBranchesAsync(request.UserId);
                await _branchRepo.SaveChangesAsync();

                if (oldOwnerId != request.UserId && await _gymRepo.CountGymsByOwnerIdAsync(oldOwnerId) == 0)
                {
                    await _gymRepo.RemoveUserRoleAsync(oldOwnerId, role.RoleId);
                    await _gymRepo.SaveChangesAsync();
                }
            }
            else if (request.RoleName == "Staff")
            {
                var branch = await _branchRepo.GetByIdAsync(request.BranchId!.Value);
                if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

                await _branchRepo.RemoveStaffFromAllBranchesAsync(request.UserId);
                await _branchRepo.SaveChangesAsync();
                var branchStaff = new BranchStaff
                {
                    StaffId = request.UserId,
                    BranchId = request.BranchId.Value,
                    AssignedAt = DateTimeHelper.GetVietnamTime()
                };
                await _branchRepo.AddBranchStaffAsync(branchStaff);
                await _branchRepo.SaveChangesAsync();
            }

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
