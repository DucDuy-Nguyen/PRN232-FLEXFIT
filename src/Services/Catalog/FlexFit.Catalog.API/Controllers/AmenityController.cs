using FlexFit.Catalog.Service.DTOs;
using FlexFit.Catalog.Service.Interfaces;
using FlexFit.Catalog.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlexFit.Catalog.API.Controllers;

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

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllAmenities()
    {
        var dtos = await _branchService.GetAllAmenitiesAsync();
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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

