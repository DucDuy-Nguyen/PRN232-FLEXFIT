using Flexfit.DTOs;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        /// <summary>
        /// 🔐 API dành riêng cho Admin cập nhật tên tiện ích
        /// </summary>
        [HttpPut("{amenityId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAmenity(Guid amenityId, [FromBody] string newAmenityName)
        {
            try
            {
                await _branchService.UpdateAmenityAsync(amenityId, newAmenityName);
                return Ok(new { message = "Cập nhật tiện ích thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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

        /// <summary>
        /// 🔐 API dành riêng cho Admin xóa tiện ích khỏi hệ thống
        /// </summary>
        [HttpDelete("{amenityId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAmenity(Guid amenityId)
        {
            try
            {
                await _branchService.DeleteAmenityAsync(amenityId);
                return Ok(new { message = "Xóa tiện ích thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}