using Flexfit.DTOs.Promotion;
using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/promotions")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách chương trình khuyến mãi.
        /// </summary>
        /// <param name="isActiveOnly">Nếu truyền true, chỉ lấy các mã đang trong thời hạn áp dụng</param>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActiveOnly)
        {
            try
            {
                var result = await _promotionService.GetAllPromotionsAsync(isActiveOnly);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một chương trình khuyến mãi theo ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _promotionService.GetPromotionByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// [Admin/Staff] Tạo mới một chương trình khuyến mãi hệ thống.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Partner,GymPartner")] // Cho phép Partner và GymPartner tạo
        public async Task<IActionResult> Create([FromBody] CreatePromotionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _promotionService.CreatePromotionAsync(request);

                // Trả về mã 201 Created kèm link định danh chi tiết của phần tử vừa tạo
                return CreatedAtAction(nameof(GetById), new { id = result.PromotionId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// [Admin] Xóa bỏ một chương trình khuyến mãi ra khỏi hệ thống.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Partner,GymPartner")] // Cho phép Partner và GymPartner xóa
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var isDeleted = await _promotionService.DeletePromotionAsync(id);
                if (!isDeleted)
                {
                    return NotFound(new { Message = "Không tìm thấy chương trình khuyến mãi cần xóa hoặc ID không hợp lệ." });
                }

                return Ok(new { Message = "Xóa chương trình khuyến mãi thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}