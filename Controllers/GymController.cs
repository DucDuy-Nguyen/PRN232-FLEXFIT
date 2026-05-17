using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flexfit.Controllers
{
    [Route("api/gyms")]
    [ApiController]
    public class GymController : ControllerBase
    {
        private readonly IGymRepository _gymRepo;
        private readonly FlexFitDbContext _context; // Bổ sung DbContext để xử lý phân quyền

        public GymController(IGymRepository gymRepo, FlexFitDbContext context)
        {
            _gymRepo = gymRepo;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGyms()
        {
            var gyms = await _gymRepo.GetAllAsync();
            var dtos = gyms.Select(g => new GymDto
            {
                GymId = g.GymId,
                OwnerId = g.OwnerId,
                GymName = g.GymName,
                Description = g.Description,
                ThumbnailUrl = g.ThumbnailUrl,
                PhoneNumber = g.PhoneNumber,
                Email = g.Email,
                Status = g.Status,
                RatingAverage = g.RatingAverage,
                TotalReviews = g.TotalReviews,
                CreatedAt = g.CreatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGymById(Guid id)
        {
            var g = await _gymRepo.GetByIdAsync(id);
            if (g == null) return NotFound(new { message = "Không tìm thấy phòng tập." });

            return Ok(new GymDto
            {
                GymId = g.GymId,
                OwnerId = g.OwnerId,
                GymName = g.GymName,
                Description = g.Description,
                ThumbnailUrl = g.ThumbnailUrl,
                PhoneNumber = g.PhoneNumber,
                Email = g.Email,
                Status = g.Status,
                RatingAverage = g.RatingAverage,
                TotalReviews = g.TotalReviews,
                CreatedAt = g.CreatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateGym(CreateGymRequest request)
        {
            // 1. Tạo phòng tập mới
            var newGym = new Gym
            {
                GymId = Guid.NewGuid(),
                OwnerId = request.OwnerId,
                GymName = request.GymName,
                Description = request.Description,
                ThumbnailUrl = request.ThumbnailUrl,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Status = "Pending", // Mặc định chờ duyệt
                RatingAverage = 0,
                TotalReviews = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _gymRepo.AddAsync(newGym);

            // ====================================================================
            // 2. TỰ ĐỘNG NÂNG CẤP QUYỀN CHO NGƯỜI TẠO THÀNH "GymPartner"
            // ====================================================================
            var partnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "GymPartner");
            if (partnerRole != null)
            {
                // Kiểm tra xem người này đã là Partner chưa (vì 1 người có thể tạo nhiều Gym)
                var hasPartnerRole = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == request.OwnerId && ur.RoleId == partnerRole.RoleId);

                if (!hasPartnerRole)
                {
                    var newUserRole = new UserRole
                    {
                        UserId = request.OwnerId,
                        RoleId = partnerRole.RoleId,
                        AssignedAt = DateTime.UtcNow
                    };
                    await _context.UserRoles.AddAsync(newUserRole);
                    await _context.SaveChangesAsync(); // Lưu thay đổi vào bảng UserRoles
                }
            }

            return Ok(new { message = "Tạo phòng tập thành công và đã tự động cấp quyền quản lý (GymPartner) cho chủ sở hữu!", gymId = newGym.GymId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGym(Guid id, UpdateGymRequest request)
        {
            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) return NotFound(new { message = "Không tìm thấy phòng tập." });

            gym.GymName = request.GymName;
            gym.Description = request.Description;
            gym.ThumbnailUrl = request.ThumbnailUrl;
            gym.PhoneNumber = request.PhoneNumber;
            gym.Email = request.Email;
            gym.UpdatedAt = DateTime.UtcNow;

            await _gymRepo.UpdateAsync(gym);
            return Ok(new { message = "Cập nhật thông tin phòng tập thành công!" });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeGymStatus(Guid id, [FromBody] string status)
        {
            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) return NotFound(new { message = "Không tìm thấy phòng tập." });

            gym.Status = status;
            gym.UpdatedAt = DateTime.UtcNow;

            await _gymRepo.UpdateAsync(gym);
            return Ok(new { message = $"Đã chuyển trạng thái phòng tập thành: {status}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGym(Guid id)
        {
            var gym = await _gymRepo.GetByIdAsync(id);
            if (gym == null) return NotFound(new { message = "Không tìm thấy phòng tập." });

            await _gymRepo.DeleteAsync(id);
            return Ok(new { message = "Xóa phòng tập thành công!" });
        }
        [HttpPut("transfer-owner")] // Đã bỏ {id} trên URL, giờ API sẽ gọn gàng hơn
        public async Task<IActionResult> TransferGymOwnership([FromBody] TransferGymOwnershipDto request)
        {
            // 1. Kiểm tra phòng tập có tồn tại không dựa vào GymId gửi từ Body
            var gym = await _gymRepo.GetByIdAsync(request.GymId);
            if (gym == null) return NotFound(new { message = "Không tìm thấy phòng tập." });

            // 2. Kiểm tra chủ sở hữu mới có tồn tại trên hệ thống không
            var newOwner = await _context.Users.FindAsync(request.NewOwnerId);
            if (newOwner == null) return NotFound(new { message = "Người dùng được chọn làm chủ sở hữu mới không tồn tại." });

            // 3. Nếu chọn lại đúng người chủ hiện tại thì báo lỗi luôn
            if (gym.OwnerId == request.NewOwnerId)
                return Ok(new { message = $"Người dùng {newOwner.FullName} hiện đã là chủ sở hữu của phòng tập này rồi." });

            // Lưu lại ID người chủ cũ để lát nữa xử lý quyền
            Guid oldOwnerId = gym.OwnerId;

            // ==========================================
            // 4. TIẾN HÀNH ĐỔI CHỦ SỞ HỮU
            // ==========================================
            gym.OwnerId = request.NewOwnerId;
            gym.UpdatedAt = DateTime.UtcNow;
            await _gymRepo.UpdateAsync(gym);

            // ==========================================
            // 5. XỬ LÝ PHÂN QUYỀN (THÊM CHO NGƯỜI MỚI, THU HỒI CỦA NGƯỜI CŨ)
            // ==========================================
            var partnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "GymPartner");
            if (partnerRole != null)
            {
                // 5.1. Cấp quyền GymPartner cho NGƯỜI MỚI (Nếu họ chưa có)
                var newOwnerHasRole = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == request.NewOwnerId && ur.RoleId == partnerRole.RoleId);

                if (!newOwnerHasRole)
                {
                    await _context.UserRoles.AddAsync(new UserRole
                    {
                        UserId = request.NewOwnerId,
                        RoleId = partnerRole.RoleId,
                        AssignedAt = DateTime.UtcNow
                    });
                }

                // 5.2. Kiểm tra NGƯỜI CŨ: Xem họ còn sở hữu Gym nào khác không?
                var oldOwnerRemainingGyms = await _context.Gyms
                    .CountAsync(g => g.OwnerId == oldOwnerId);

                // Nếu họ không còn sở hữu Gym nào (Count == 0), tiến hành thu hồi quyền
                if (oldOwnerRemainingGyms == 0)
                {
                    var oldUserRole = await _context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserId == oldOwnerId && ur.RoleId == partnerRole.RoleId);

                    if (oldUserRole != null)
                    {
                        _context.UserRoles.Remove(oldUserRole);
                    }
                }

                // Lưu các thay đổi về quyền (UserRoles) xuống Database
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = $"Đã chuyển nhượng quyền sở hữu phòng tập sang cho {newOwner.FullName} thành công và cập nhật lại hệ thống phân quyền!" });
        }

    }
}