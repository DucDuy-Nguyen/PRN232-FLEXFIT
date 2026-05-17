using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Flexfit.Controllers
{
    [Route("api/branches")]
    [ApiController]
    public class BranchController : ControllerBase
    {
        private readonly IBranchRepository _branchRepo;
        private readonly FlexFitDbContext _context; // Bổ sung context song song để chạy API assign-staff

        // Constructor nhận cả Repo cũ và Context mới, giữ nguyên cấu trúc
        public BranchController(IBranchRepository branchRepo, FlexFitDbContext context)
        {
            _branchRepo = branchRepo;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBranches()
        {
            var branches = await _branchRepo.GetAllAsync();
            var dtos = branches.Select(b => new BranchDto
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
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                // MAP DỮ LIỆU STAFFS VÀO ĐÂY:
                Staffs = b.BranchStaffs.Select(bs => new StaffInfoDto
                {
                    StaffId = bs.StaffId,
                    FullName = bs.Staff.FullName // Đã lấy được nhờ hàm Include ở tầng Repo
                }).ToList()
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(Guid id)
        {
            var b = await _branchRepo.GetByIdAsync(id);
            if (b == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            return Ok(new BranchDto
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
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                // MAP DỮ LIỆU STAFFS VÀO ĐÂY:
                Staffs = b.BranchStaffs.Select(bs => new StaffInfoDto
                {
                    StaffId = bs.StaffId,
                    FullName = bs.Staff.FullName
                }).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBranch(CreateBranchRequest request)
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
                IsActive = true, // Mặc định mở cửa
                CreatedAt = DateTime.UtcNow
            };

            await _branchRepo.AddAsync(newBranch);
            return Ok(new { message = "Tạo chi nhánh thành công!", branchId = newBranch.BranchId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(Guid id, UpdateBranchRequest request)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            branch.BranchName = request.BranchName;
            branch.Address = request.Address;
            branch.City = request.City;
            branch.District = request.District;
            branch.OpenTime = request.OpenTime;
            branch.CloseTime = request.CloseTime;
            branch.ThumbnailUrl = request.ThumbnailUrl;
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepo.UpdateAsync(branch);
            return Ok(new { message = "Cập nhật thông tin chi nhánh thành công!" });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeBranchStatus(Guid id, [FromBody] bool isActive)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            branch.IsActive = isActive;
            branch.UpdatedAt = DateTime.UtcNow;

            await _branchRepo.UpdateAsync(branch);
            string statusMsg = isActive ? "Hoạt động" : "Tạm ngưng";
            return Ok(new { message = $"Đã chuyển trạng thái chi nhánh thành: {statusMsg}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranch(Guid id)
        {
            var branch = await _branchRepo.GetByIdAsync(id);
            if (branch == null) return NotFound(new { message = "Không tìm thấy chi nhánh." });

            await _branchRepo.DeleteAsync(id);
            return Ok(new { message = "Xóa chi nhánh thành công!" });
        }

        [HttpPost("assign-staff")] // Đã gỡ bỏ [Authorize(Roles = "GymPartner")] - Front-end tự kiểm soát giao diện hiển thị nút bấm
        public async Task<IActionResult> AssignStaffToBranch([FromBody] AssignStaffDto dto)
        {
            // 1. Kiểm tra xem Chi nhánh (Branch) này có tồn tại thật không
            var branch = await _context.Branches.FindAsync(dto.BranchId);
            if (branch == null)
                return NotFound(new { message = "Chi nhánh không tồn tại trên hệ thống." });

            // 2. Kiểm tra xem người được chọn làm nhân viên có tồn tại trong hệ thống không
            var employee = await _context.Users.FindAsync(dto.UserId);
            if (employee == null)
                return NotFound(new { message = "Người dùng được chọn làm nhân viên không tồn tại." });

            // 3. Tiến hành gán quyền: Kiểm tra xem họ đã có Role là 'Staff' chuyên biệt chưa
            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
            if (staffRole == null)
                return BadRequest(new { message = "Hệ thống chưa cấu hình vai trò 'Staff' trong DB!" });

            var hasStaffRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == dto.UserId && ur.RoleId == staffRole.RoleId);

            if (!hasStaffRole)
            {
                var oldRoles = _context.UserRoles.Where(ur => ur.UserId == dto.UserId);
                _context.UserRoles.RemoveRange(oldRoles);

                var newUserRole = new UserRole
                {
                    UserId = dto.UserId,
                    RoleId = staffRole.RoleId,
                    AssignedAt = DateTime.UtcNow
                };
                await _context.UserRoles.AddAsync(newUserRole);
            }

            // 4. Kiểm tra xem người này đã được xếp vào chính chi nhánh này chưa để tránh trùng bản ghi
            var isAlreadyStaffHere = await _context.BranchStaffs
                .AnyAsync(bs => bs.StaffId == dto.UserId && bs.BranchId == dto.BranchId);

            if (isAlreadyStaffHere)
                return BadRequest(new { message = "Người này đã là nhân viên của chi nhánh này rồi!" });

            var oldBranchAssignments = _context.BranchStaffs.Where(bs => bs.StaffId == dto.UserId);
            _context.BranchStaffs.RemoveRange(oldBranchAssignments);

            var newBranchStaff = new BranchStaff
            {
                StaffId = dto.UserId,
                BranchId = dto.BranchId,
                AssignedAt = DateTime.UtcNow
            };
            await _context.BranchStaffs.AddAsync(newBranchStaff);

            // 5. Lưu tất cả thay đổi xuống Database
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã bổ nhiệm nhân viên {employee.FullName} vào làm việc tại chi nhánh {branch.BranchName} thành công!" });
        }
        [HttpDelete("remove-staff")] // Front-end gọi khi nhấn nút xóa/gỡ nhân viên
        public async Task<IActionResult> RemoveStaffFromBranch([FromQuery] Guid staffId, [FromQuery] Guid branchId)
        {
            // 1. Tìm bản ghi phân bổ nhân viên trong bảng trung gian BranchStaffs
            var branchStaff = await _context.BranchStaffs
                .FirstOrDefaultAsync(bs => bs.StaffId == staffId && bs.BranchId == branchId);

            if (branchStaff == null)
                return NotFound(new { message = "Nhân viên này hiện không thuộc chi nhánh này hoặc không tồn tại bản ghi bổ nhiệm." });

            // 2. Xóa liên kết tại chi nhánh hiện tại
            _context.BranchStaffs.Remove(branchStaff);

            // 3. THỰC HIỆN LUÔN: Kiểm tra xem người này còn thuộc chi nhánh nào khác không
            // (Vì lệnh Remove ở trên chưa SaveChanges nên cần đếm các chi nhánh KHÁC chi nhánh hiện tại)
            var remainingBranchesCount = await _context.BranchStaffs
                .CountAsync(bs => bs.StaffId == staffId && bs.BranchId != branchId);

            // Nếu không còn làm ở chi nhánh nào khác nữa, tiến hành thu hồi quyền Staff
            if (remainingBranchesCount == 0)
            {
                var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
                if (staffRole != null)
                {
                    // Tìm quyền Staff hiện tại của user này trong bảng UserRoles và xóa đi
                    var userStaffRole = await _context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserId == staffId && ur.RoleId == staffRole.RoleId);

                    if (userStaffRole != null)
                    {
                        _context.UserRoles.Remove(userStaffRole);
                    }
                }
            }

            // 4. Lưu tất cả thay đổi xuống DB
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gỡ nhân viên ra khỏi chi nhánh và cập nhật lại quyền hạn tài khoản thành công!" });
        }

        [HttpPut("update-staff")]
        public async Task<IActionResult> UpdateBranchStaff([FromBody] UpdateBranchStaffDto dto)
        {
            // 1. Kiểm tra chi nhánh có tồn tại không
            var branch = await _context.Branches.FindAsync(dto.BranchId);
            if (branch == null) return NotFound(new { message = "Chi nhánh không tồn tại." });

            // 2. Kiểm tra người mới có tồn tại không
            var newEmployee = await _context.Users.FindAsync(dto.NewStaffId);
            if (newEmployee == null) return NotFound(new { message = "Nhân viên mới được chọn không tồn tại." });

            // 3. TỰ ĐỘNG TÌM VÀ XỬ LÝ NGƯỜI CŨ 
            var oldAssignment = await _context.BranchStaffs
                .FirstOrDefaultAsync(bs => bs.BranchId == dto.BranchId);

            if (oldAssignment != null)
            {
                Guid oldStaffId = oldAssignment.StaffId;

                // Bỏ qua nếu chọn lại người cũ
                if (oldStaffId == dto.NewStaffId)
                {
                    return Ok(new { message = $"Nhân viên {newEmployee.FullName} đã đang quản lý chi nhánh này rồi!" });
                }

                // Gỡ người cũ ra
                _context.BranchStaffs.Remove(oldAssignment);

                // Kiểm tra và thu hồi quyền nếu người cũ không còn làm ở chi nhánh nào
                var oldStaffRemainingCount = await _context.BranchStaffs
                    .CountAsync(bs => bs.StaffId == oldStaffId && bs.BranchId != dto.BranchId);

                if (oldStaffRemainingCount == 0)
                {
                    var staffRoleName = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
                    if (staffRoleName != null)
                    {
                        var oldUserStaffRole = await _context.UserRoles
                            .FirstOrDefaultAsync(ur => ur.UserId == oldStaffId && ur.RoleId == staffRoleName.RoleId);
                        if (oldUserStaffRole != null)
                        {
                            _context.UserRoles.Remove(oldUserStaffRole);
                        }
                    }
                }
            }

            // 4. XỬ LÝ NGƯỜI MỚI: Đảm bảo họ có quyền 'Staff' 
            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
            if (staffRole == null) return BadRequest(new { message = "Hệ thống chưa cấu hình vai trò 'Staff' trong DB!" });

            var hasStaffRole = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == dto.NewStaffId && ur.RoleId == staffRole.RoleId);

            if (!hasStaffRole)
            {
                var oldRolesOfNewStaff = _context.UserRoles.Where(ur => ur.UserId == dto.NewStaffId);
                _context.UserRoles.RemoveRange(oldRolesOfNewStaff);

                var newUserRole = new UserRole
                {
                    UserId = dto.NewStaffId,
                    RoleId = staffRole.RoleId,
                    AssignedAt = DateTime.UtcNow
                };
                await _context.UserRoles.AddAsync(newUserRole);
            }

            // Gỡ liên kết cũ của người mới (nếu họ đang làm ở chi nhánh khác) để dời về đây
            var oldBranchAssignmentsOfNewStaff = _context.BranchStaffs.Where(bs => bs.StaffId == dto.NewStaffId);
            _context.BranchStaffs.RemoveRange(oldBranchAssignmentsOfNewStaff);

            // 5. Chèn bản ghi bổ nhiệm mới
            var newBranchStaff = new BranchStaff
            {
                StaffId = dto.NewStaffId,
                BranchId = dto.BranchId,
                AssignedAt = DateTime.UtcNow
            };
            await _context.BranchStaffs.AddAsync(newBranchStaff);

            // 6. Lưu toàn bộ thay đổi xuống Database
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã tự động thay thế và chuyển giao quyền quản lý chi nhánh {branch.BranchName} sang cho {newEmployee.FullName} thành công!" });
        }

    }
}