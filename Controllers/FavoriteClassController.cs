using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/favorite-classes")]
    [ApiController]
    [Authorize(Roles = "Member")] // Chỉ hội viên mới có quyền "thích" lớp học
    public class FavoriteClassController : ControllerBase
    {
        private readonly IFavoriteClassService _favoriteService;

        public FavoriteClassController(IFavoriteClassService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) throw new Exception("Không tìm thấy thông tin định danh hội viên.");
            return Guid.Parse(userIdStr);
        }

        /// <summary>
        /// [Member] Thêm hoặc Hủy yêu thích một lớp học (Cơ chế Toggle)
        /// </summary>
        [HttpPost("toggle/{classId}")]
        public async Task<IActionResult> ToggleFavorite(Guid classId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _favoriteService.ToggleFavoriteClassAsync(userId, classId);
                return Ok(new { Message = message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// [Member] Lấy danh sách toàn bộ các lớp học mà hội viên này đã bấm thích
        /// </summary>
        [HttpGet("my-list")]
        public async Task<IActionResult> GetMyFavorites()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _favoriteService.GetMyFavoriteClassesAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}