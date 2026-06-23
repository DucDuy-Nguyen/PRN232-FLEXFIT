using Flexfit.DTOs;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/amenities")]
    [ApiController]
    [Authorize]
    public class AmenityController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public AmenityController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        /// <summary>
        /// 🔓 API lấy toàn bộ danh sách tiện ích hệ thống (Dành cho GymPartner/Staff chọn, hoặc hiển thị UI)
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Cho phép tất cả mọi người (kể cả khách) xem danh mục tiện ích nếu cần
        public async Task<IActionResult> GetAllAmenities()
        {
            var dtos = await _branchService.GetAllAmenitiesAsync();
            return Ok(dtos);
        }

        /// <summary>
        /// 🔐 API dành riêng cho Admin tạo mới tiện ích gốc vào hệ thống
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")] // Chỉ tài khoản có Role là Admin mới được quyền tạo
        public async Task<IActionResult> CreateAmenity([FromBody] string amenityName)
        {
            try
            {
                var amenityId = await _branchService.CreateAmenityAsync(amenityName);
                return Ok(new { message = "Tạo danh mục tiện ích thành công!", amenityId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}