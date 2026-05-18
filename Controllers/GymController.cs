using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/gyms")]
    [ApiController]
    public class GymController : ControllerBase
    {
        private readonly IGymService _gymService;

        public GymController(IGymService gymService)
        {
            _gymService = gymService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGyms()
        {
            var dtos = await _gymService.GetAllGymsAsync();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGymById(Guid id)
        {
            var dto = await _gymService.GetGymByIdAsync(id);
            if (dto == null) return NotFound(new { message = "Không tìm thấy phòng tập." });
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGym([FromBody] CreateGymRequest request)
        {
            var gymId = await _gymService.CreateGymAsync(request);
            return Ok(new { message = "Tạo phòng tập và tự động cấp quyền thành công!", gymId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGym(Guid id, [FromBody] UpdateGymRequest request)
        {
            try
            {
                await _gymService.UpdateGymAsync(id, request);
                return Ok(new { message = "Cập nhật thông tin phòng tập thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeGymStatus(Guid id, [FromBody] string status)
        {
            try
            {
                await _gymService.ChangeGymStatusAsync(id, status);
                return Ok(new { message = $"Đã chuyển trạng thái phòng tập thành: {status}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGym(Guid id)
        {
            try
            {
                await _gymService.DeleteGymAsync(id);
                return Ok(new { message = "Xóa phòng tập thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("transfer-owner")]
        public async Task<IActionResult> TransferGymOwnership([FromBody] TransferGymOwnershipDto request)
        {
            try
            {
                await _gymService.TransferGymOwnershipAsync(request);
                return Ok(new { message = "Đã chuyển nhượng quyền sở hữu phòng tập thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message }); // Trả về 404 nếu không thấy Gym hoặc User mới
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message }); // Trả về 400 nếu chọn trùng chủ sở hữu cũ
            }
        }
    }
}