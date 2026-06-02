using Flexfit.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Flexfit.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize] // Phải đăng nhập mới xem được thông báo của mình
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdValue)) throw new Exception("Unauthorized.");
            return Guid.Parse(userIdValue);
        }

        // Lấy toàn bộ thông báo của Member hiện tại
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var list = await _notificationService.GetMyNotificationsAsync(GetCurrentUserId());
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API Đánh dấu một thông báo là đã đọc
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id, GetCurrentUserId());
                return Ok(new { message = "Đã đánh dấu đọc thông báo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // API Đánh dấu ĐỌC TẤT CẢ thông báo của Member hiện tại
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                await _notificationService.MarkAllAsReadAsync(GetCurrentUserId());
                return Ok(new { message = "Đã đánh dấu đọc tất cả thông báo thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}