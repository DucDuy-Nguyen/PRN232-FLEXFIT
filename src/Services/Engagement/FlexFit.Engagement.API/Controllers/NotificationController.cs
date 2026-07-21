using FlexFit.Engagement.API.DTOs.Notifications;
using FlexFit.Engagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FlexFit.Engagement.API.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value
                          ?? User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdValue)) throw new UnauthorizedAccessException("Unauthorized.");
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

    // Đánh dấu một thông báo là đã đọc
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

    // Đánh dấu ĐỌC TẤT CẢ thông báo của Member hiện tại
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

    // API DÀNH RIÊNG CHO ADMIN
    [HttpPost("admin/create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminCreateNotification([FromBody] AdminCreateNotificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _notificationService.SendAdminNotificationAsync(request);
            if (result)
            {
                return Ok(new { message = "Admin tạo và gửi thông báo thành công." });
            }
            return BadRequest(new { message = "Không thể gửi thông báo." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Đã xảy ra lỗi hệ thống: {ex.Message}" });
        }
    }
}
