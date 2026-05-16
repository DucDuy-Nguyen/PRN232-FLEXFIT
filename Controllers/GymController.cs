using Flexfit.DTOs;
using Flexfit.Models;
using Flexfit.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/gyms")]
    [ApiController]
    public class GymController : ControllerBase
    {
        private readonly IGymRepository _gymRepo;
        public GymController(IGymRepository gymRepo) => _gymRepo = gymRepo;

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
            return Ok(new { message = "Tạo phòng tập thành công!", gymId = newGym.GymId });
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
    }
}