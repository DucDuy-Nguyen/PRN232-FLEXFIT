using FlexFit.Engagement.Application.DTOs.Promotions;
using FlexFit.Engagement.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexFit.Engagement.API.Controllers;

[Route("api/promotions")]
[ApiController]
public class PromotionController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PromotionController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActiveOnly)
    {
        try { return Ok(await _promotionService.GetAllPromotionsAsync(isActiveOnly)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await _promotionService.GetPromotionByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Partner,GymPartner")]
    public async Task<IActionResult> Create([FromBody] CreatePromotionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var result = await _promotionService.CreatePromotionAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.PromotionId }, result);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Partner,GymPartner")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _promotionService.DeletePromotionAsync(id);
        if (!deleted) return NotFound(new { message = "Không tìm thấy chương trình khuyến mãi." });
        return Ok(new { message = "Xóa chương trình khuyến mãi thành công." });
    }
}
