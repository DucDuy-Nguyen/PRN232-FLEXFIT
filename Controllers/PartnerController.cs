using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Flexfit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Flexfit.Controllers
{
    [Route("api/partner")]
    [ApiController]
    [Authorize(Roles = "GymPartner")]
    public class PartnerController : ControllerBase
    {
        private readonly IPartnerService _partnerService;
        private readonly ILogger<PartnerController> _logger;

        public PartnerController(IPartnerService partnerService, ILogger<PartnerController> logger)
        {
            _partnerService = partnerService;
            _logger = logger;
        }

        private Guid GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdValue)) return Guid.Empty;
            return Guid.Parse(userIdValue);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var ownerId = GetCurrentUserId();
            var stats = await _partnerService.GetDashboardStatsAsync(ownerId);
            return Ok(stats);
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var ownerId = GetCurrentUserId();
            var customers = await _partnerService.GetCustomersAsync(ownerId);
            return Ok(customers);
        }

       [HttpGet("reviews")]
public async Task<IActionResult> GetReviews()
{
    var ownerId = GetCurrentUserId();

    try
    {
        var reviews = await _partnerService.GetReviewsAsync(ownerId);
        return Ok(reviews);
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Error loading partner reviews for user {UserId}",
            ownerId
        );

        return StatusCode(500, new
        {
            message = "Khong the tai danh sach danh gia.",
            detail = ex.ToString()
        });
    }
}

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue()
        {
            var ownerId = GetCurrentUserId();
            var revenue = await _partnerService.GetRevenueAsync(ownerId);
            return Ok(revenue);
        }
    }
}
