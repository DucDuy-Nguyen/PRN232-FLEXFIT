using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FlexFit.Identity.API.Authorization;
using FlexFit.Identity.API.Contracts.Profiles;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Models.DTOs;

namespace FlexFit.Identity.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profiles")]
[Authorize]
public sealed class MemberProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUserService;

    public MemberProfilesController(IProfileService profileService, ICurrentUserService currentUserService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MemberProfileDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _profileService.GetByUserIdAsync(userId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MemberProfileDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _profileService.UpdateAsync(
            userId.Value,
            request.FullName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.AvatarUrl,
            request.Gender,
            request.HeightCm,
            request.WeightKg,
            request.FitnessGoal,
            request.ActivityLevel,
            request.PreferredWorkoutTime,
            request.Bio,
            cancellationToken);
        
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = IdentityPolicies.UserManagement)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MemberProfileDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(
        Guid userId, 
        CancellationToken cancellationToken)
    {
        var result = await _profileService.GetByUserIdAsync(userId, cancellationToken);
        return Ok(result);
    }
}
