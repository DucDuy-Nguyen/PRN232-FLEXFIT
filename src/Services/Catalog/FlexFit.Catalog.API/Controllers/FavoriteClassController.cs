using FlexFit.Catalog.Service.Interfaces;
using FlexFit.Catalog.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexFit.Catalog.API.Controllers;

[Route("api/favorite-classes")]
[ApiController]
[Authorize(Roles = "Member")]
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

