using FlexFit.CatalogService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Controllers;

[Route("api/favorite-gyms")]
[ApiController]
[Authorize(Roles = "Member")]
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
