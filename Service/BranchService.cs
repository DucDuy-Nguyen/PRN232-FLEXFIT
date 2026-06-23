using Flexfit.DTOs;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Flexfit.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flexfit.Service
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepo;
        private readonly INotificationService _notificationService;

        public BranchService(IBranchRepository branchRepo, INotificationService notificationService)
        {
            _branchRepo = branchRepo;
            _notificationService = notificationService;
        }

        // Hàm helper kiểm tra quyền: Phải là Chủ chi nhánh HOẶC Staff của chi nhánh đó
        private async Task<bool> CheckBranchManagementPermissionAsync(Guid branchId, Guid userId)
        {
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(branchId, userId);
            if (isOwner) return true;

            var isStaffHere = await _branchRepo.IsStaffInBranchAsync(userId, branchId);
            if (isStaffHere) return true;

            return false;
        }

        public async Task<IEnumerable<BranchDto>> GetAllBranchesAsync()
        {
            var branches = await _branchRepo.GetAllAsync();
            return branches.Select(MapToDto);
        }

        public async Task<IEnumerable<BranchDto>> GetBranchesByPartnerIdAsync(Guid ownerId)
        {
            var branches = await _branchRepo.GetByOwnerIdAsync(ownerId);
            return branches.Select(MapToDto);
        }

        public async Task<BranchDto?> GetBranchByIdAsync(Guid id)
        {
            var b = await _branchRepo.GetByIdAsync(id);
            if (b == null) return null;

            return MapToDto(b);
        }

        public async Task<Guid> CreateBranchAsync(CreateBranchRequest request, Guid currentUserId)
        {
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
            // Cho phép cả Staff phụ trách chi nhánh cập nhật thông tin chung của chi nhánh nếu cần
            var hasPermission = await CheckBranchManagementPermissionAsync(id, currentUserId);
            if (!hasPermission) throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa chi nhánh này.");

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
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(id, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền xóa chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) throw new KeyNotFoundException("Không tìm thấy chi nhánh.");

            await _branchRepo.DeleteAsync(id);
        }

        // ==========================================================
        // KHU VỰC QUẢN LÝ TIỆN ÍCH (AMENITIES) CHI NHÁNH
        // ==========================================================
        public async Task UpdateBranchAmenitiesAsync(Guid branchId, UpdateBranchAmenitiesRequest request, Guid currentUserId)
        {
            // 🛑 CHECK QUYỀN: GymPartner (Owner) HOẶC Staff được assign vào chi nhánh mới có quyền add/bớt tiện ích
            var hasPermission = await CheckBranchManagementPermissionAsync(branchId, currentUserId);
            if (!hasPermission) throw new UnauthorizedAccessException("Bạn không có quyền quản lý tiện ích tại chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

            // Clear các tiện ích cũ đang liên kết và nạp lại danh sách mới
            if (branch.Amenities == null)
            {
                branch.Amenities = new List<GymAmenity>();
            }
            else
            {
                branch.Amenities.Clear();
            }

            if (request.AmenityIds != null && request.AmenityIds.Any())
            {
                foreach (var amenityId in request.AmenityIds)
                {
                    // Giả sử repository của bạn có hàm lấy thực thể Amenity gốc từ Database hoặc viết trực tiếp qua DbContext
                    var amenity = await _branchRepo.GetAmenityByIdAsync(amenityId);
                    if (amenity != null)
                    {
                        branch.Amenities.Add(amenity);
                    }
                }
            }

            branch.UpdatedAt = DateTimeHelper.GetVietnamTime();
            // Lưu thay đổi
            await _branchRepo.SaveChangesAsync();
        }

        // ==========================================================
        // KHU VỰC THAY ĐỔI NHÂN SỰ
        // ==========================================================
        public async Task AssignStaffToBranchAsync(AssignStaffDto dto, Guid currentUserId)
        {
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

            try
            {
                await _notificationService.SendAsync(
                    dto.UserId,
                    "Bạn có nhiệm vụ mới! 💼",
                    $"Bạn đã được bổ nhiệm làm nhân viên (Staff) quản lý tại chi nhánh [{branch.BranchName}]. Hãy kiểm tra hệ thống.",
                    "StaffAssignment"
                );
            }
            catch { }
        }

        public async Task AssignStaffToBranchByEmailAsync(AssignStaffByEmailDto dto, Guid currentUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentException("Vui lòng nhập email nhân viên.");

            var partnerRole = await _branchRepo.GetRoleByNameAsync("GymPartner");
            if (partnerRole == null || !await _branchRepo.UserHasRoleAsync(currentUserId, partnerRole.RoleId))
            {
                throw new UnauthorizedAccessException("Chỉ GymPartner mới được phân công nhân viên.");
            }

            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(dto.BranchId, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền quản lý nhân sự tại chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại trên hệ thống.");

            var employee = await _branchRepo.GetUserByEmailAsync(dto.Email);
            if (employee == null) throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

            var roleNames = employee.UserRoles
                .Select(ur => ur.Role?.RoleName)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .Select(roleName => roleName!)
                .ToList();

            if (roleNames.Contains("Admin", StringComparer.OrdinalIgnoreCase) || roleNames.Contains("GymPartner", StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Không thể thêm tài khoản Admin hoặc GymPartner làm nhân viên.");
            }

            var hasOtherBranchAssignment = employee.BranchStaffs.Any(bs => bs.BranchId != dto.BranchId);
            if (hasOtherBranchAssignment) throw new ArgumentException("Tài khoản này đã là nhân viên của phòng gym khác.");

            var staffRole = await _branchRepo.GetRoleByNameAsync("Staff");
            if (staffRole == null) throw new ArgumentException("Hệ thống chưa cấu hình vai trò 'Staff' trong DB!");

            var hasStaffRole = roleNames.Contains("Staff", StringComparer.OrdinalIgnoreCase) || await _branchRepo.UserHasRoleAsync(employee.UserId, staffRole.RoleId);
            if (!hasStaffRole)
            {
                await _branchRepo.AddUserRoleAsync(new UserRole
                {
                    UserId = employee.UserId,
                    RoleId = staffRole.RoleId,
                    AssignedAt = DateTimeHelper.GetVietnamTime()
                });
            }

            await _branchRepo.RemoveStaffFromAllBranchesAsync(employee.UserId);
            await _branchRepo.SaveChangesAsync();

            await _branchRepo.AddBranchStaffAsync(new BranchStaff
            {
                StaffId = employee.UserId,
                BranchId = dto.BranchId,
                AssignedAt = DateTimeHelper.GetVietnamTime()
            });

            await _branchRepo.SaveChangesAsync();

            try
            {
                await _notificationService.SendAsync(
                    employee.UserId,
                    "Bạn có nhiệm vụ mới!",
                    $"Bạn đã được bổ nhiệm làm nhân viên (Staff) quản lý tại chi nhánh [{branch.BranchName}]. Hãy kiểm tra hệ thống.",
                    "StaffAssignment"
                );
            }
            catch { }
        }

        public async Task RemoveStaffFromBranchAsync(Guid staffId, Guid branchId, Guid currentUserId)
        {
            var isOwner = await _branchRepo.CheckBranchOwnershipAsync(branchId, currentUserId);
            if (!isOwner) throw new UnauthorizedAccessException("Bạn không có quyền gỡ nhân sự tại chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

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

            try
            {
                await _notificationService.SendAsync(
                    staffId,
                    "Thay đổi nhân sự chi nhánh ⚠️",
                    $"Quyền quản lý (Staff) của bạn tại chi nhánh [{branch.BranchName}] đã được gỡ bỏ bởi Chủ hệ thống.",
                    "StaffRevocation"
                );
            }
            catch { }
        }

        public async Task UpdateBranchStaffAsync(UpdateBranchStaffDto dto, Guid currentUserId)
        {
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

                try
                {
                    await _notificationService.SendAsync(
                        oldStaffId,
                        "Thay đổi nhân sự chi nhánh ⚠️",
                        $"Bạn đã được gỡ khỏi vị trí phụ trách chi nhánh [{branch.BranchName}] do có sự chuyển giao nhân sự.",
                        "StaffRevocation"
                    );
                }
                catch { }
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

            try
            {
                await _notificationService.SendAsync(
                    dto.NewStaffId,
                    "Bạn có nhiệm vụ mới! 💼",
                    $"Bạn đã được bổ nhiệm phụ trách quản lý chi nhánh [{branch.BranchName}]. Hãy bắt đầu ca làm việc của mình.",
                    "StaffAssignment"
                );
            }
            catch { }
        }
        public async Task<IEnumerable<GymAmenityDto>> GetAllAmenitiesAsync()
        {
            var amenities = await _branchRepo.GetAllAmenitiesAsync();
            return amenities.Select(a => new GymAmenityDto
            {
                AmenityId = a.AmenityId,
                AmenityName = a.AmenityName
            });
        }

        public async Task<Guid> CreateAmenityAsync(string amenityName)
        {
            if (string.IsNullOrWhiteSpace(amenityName))
                throw new ArgumentException("Tên tiện ích không được để trống.");

            var exists = await _branchRepo.AmenityExistsAsync(amenityName);
            if (exists)
                throw new ArgumentException("Tên tiện ích này đã tồn tại trên hệ thống.");

            var newAmenity = new GymAmenity
            {
                AmenityId = Guid.NewGuid(),
                AmenityName = amenityName.Trim()
            };

            await _branchRepo.AddAmenityAsync(newAmenity);
            return newAmenity.AmenityId;
        }
        // ==========================================================
        // KHU VỰC QUẢN LÝ HÌNH ẢNH (IMAGES) CHI NHÁNH
        // ==========================================================
        public async Task UpdateBranchImagesAsync(Guid branchId, UpdateBranchImagesRequest request, Guid currentUserId)
        {
            // 1. Kiểm tra quyền và sự tồn tại
            var hasPermission = await CheckBranchManagementPermissionAsync(branchId, currentUserId);
            if (!hasPermission) throw new UnauthorizedAccessException("Bạn không có quyền quản lý hình ảnh tại chi nhánh này.");

            var branch = await _branchRepo.GetByIdAsync(branchId);
            if (branch == null) throw new KeyNotFoundException("Chi nhánh không tồn tại.");

            // 2. Tạo danh sách ảnh mới (Chỉ tạo List ở ngoài, KHÔNG gán đè vào branch.BranchImages)
            var newImages = new List<BranchImage>();
            if (request.Images != null && request.Images.Any())
            {
                foreach (var imgReq in request.Images)
                {
                    if (string.IsNullOrWhiteSpace(imgReq.ImageUrl)) continue;

                    newImages.Add(new BranchImage
                    {
                        BranchImageId = Guid.NewGuid(),
                        BranchId = branchId,
                        ImageUrl = imgReq.ImageUrl.Trim(),
                        DisplayOrder = imgReq.DisplayOrder
                    });
                }
            }

            // 3. Cập nhật thời gian cho nhánh
            branch.UpdatedAt = DateTimeHelper.GetVietnamTime();

            // 4. 🚀 GỌI XUỐNG REPO ĐỂ THỰC THI (Fix triệt để lỗi 500)
            await _branchRepo.UpdateBranchImagesDbAsync(branchId, newImages);
        }

        // ==========================================================
        // MAPPER DỮ LIỆU ĐẦU RA (ĐÃ BAO GỒM TIỆN ÍCH AMENITIES)
        // ==========================================================
        private static BranchDto MapToDto(Branch b)
        {
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
                Staffs = b.BranchStaffs?.Select(bs => new StaffInfoDto
                {
                    StaffId = bs.StaffId,
                    FullName = bs.Staff?.FullName ?? "N/A"
                }).ToList() ?? new List<StaffInfoDto>(),

                Amenities = b.Amenities?.Select(a => new GymAmenityDto
                {
                    AmenityId = a.AmenityId,
                    AmenityName = a.AmenityName
                }).ToList() ?? new List<GymAmenityDto>(),

                // 📸 Ánh xạ danh sách hình ảnh trả về cho Client hiển thị
                Images = b.BranchImages?.Select(i => new BranchImageDto
                {
                    BranchImageId = i.BranchImageId,
                    ImageUrl = i.ImageUrl,
                    DisplayOrder = i.DisplayOrder
                }).OrderBy(i => i.DisplayOrder).ToList() ?? new List<BranchImageDto>()
            };
        }
    }
}