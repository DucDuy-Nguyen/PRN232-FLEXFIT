using FlexFit.Catalog.Service.DTOs;
using FlexFit.Catalog.Service.Interfaces;
using FlexFit.Catalog.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FlexFit.Catalog.API.Controllers;

[Route("api/gyms")]
[ApiController]
[Authorize]
public class GymController : ControllerBase
{
    private readonly IGymService _gymService;

    public GymController(IGymService gymService)
    {
        _gymService = gymService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdValue)) return Guid.Empty;
        return Guid.Parse(userIdValue);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllGyms(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? ownerId,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var pagedGyms = await _gymService.GetGymsPagedAsync(search, status, ownerId, sortBy, sortDirection, pageNumber, pageSize);
        return Ok(pagedGyms);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGymById(Guid id)
    {
        var dto = await _gymService.GetGymByIdAsync(id);
        if (dto == null) return NotFound(new { message = "Không tìm thấy phòng tập." });
        return Ok(dto);
    }

    [HttpGet("partner")]
    [Authorize(Roles = "GymPartner")]
    public async Task<IActionResult> GetGymsForPartner()
    {
        var ownerId = GetCurrentUserId();
        var dtos = await _gymService.GetGymsByPartnerIdAsync(ownerId);
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateGym([FromBody] CreateGymRequest request)
    {
        try
        {
            var gymId = await _gymService.CreateGymAsync(request, GetCurrentUserId());
            return Ok(new { message = "Admin đã tạo phòng tập thành công!", gymId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "GymPartner,Admin")]
    public async Task<IActionResult> UpdateGym(Guid id, [FromBody] UpdateGymRequest request)
    {
        try
        {
            bool isAdmin = User.IsInRole("Admin");
            await _gymService.UpdateGymAsync(id, request, GetCurrentUserId(), isAdmin);
            return Ok(new { message = "Cập nhật thông tin phòng tập thành công!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Partner,GymPartner")]
    public async Task<IActionResult> ChangeGymStatus(Guid id, [FromBody] string status)
    {
        try
        {
            bool isAdmin = User.IsInRole("Admin");
            await _gymService.ChangeGymStatusAsync(id, status, GetCurrentUserId(), isAdmin);
            return Ok(new { message = $"Đã chuyển trạng thái phòng tập thành: {status}" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "GymPartner,Admin")]
    public async Task<IActionResult> DeleteGym(Guid id)
    {
        try
        {
            bool isAdmin = User.IsInRole("Admin");
            await _gymService.DeleteGymAsync(id, GetCurrentUserId(), isAdmin);
            return Ok(new { message = "Xóa phòng tập thành công!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPut("transfer-owner")]
    [Authorize(Roles = "GymPartner")]
    public async Task<IActionResult> TransferGymOwnership([FromBody] TransferGymOwnershipDto request)
    {
        try
        {
            await _gymService.TransferGymOwnershipAsync(request, GetCurrentUserId());
            return Ok(new { message = "Đã chuyển nhượng quyền sở hữu phòng tập thành công!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
}

