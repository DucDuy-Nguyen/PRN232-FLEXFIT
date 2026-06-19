using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/favorite-gyms")]
    [ApiController]
    [Authorize(Roles = "Member")] // 🚨 Chỉ hội viên mới được sử dụng tính năng yêu thích này
    public class FavoriteGymController : ControllerBase
    {
        private readonly IFavoriteGymService _favoriteService;

        public FavoriteGymController(IFavoriteGymService favoriteService)
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
        /// [Member] Bấm để Thêm hoặc Hủy yêu thích một phòng gym (Cơ chế Toggle)
        /// </summary>
        [HttpPost("toggle/{gymId}")]
        public async Task<IActionResult> ToggleFavorite(Guid gymId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var message = await _favoriteService.ToggleFavoriteGymAsync(userId, gymId);
                return Ok(new { Message = message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// [Member] Lấy danh sách tất cả các phòng gym mà hội viên này đã yêu thích
        /// </summary>
        [HttpGet("my-list")]
        public async Task<IActionResult> GetMyFavorites()
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _favoriteService.GetMyFavoriteGymsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}