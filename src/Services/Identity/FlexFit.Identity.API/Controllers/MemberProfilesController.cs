using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FlexFit.Identity.API.Authorization;
using FlexFit.Identity.API.Contracts.Profiles;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Application.Profiles;

namespace FlexFit.Identity.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profiles")]
[Authorize]
public sealed class MemberProfilesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public MemberProfilesController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
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

        var query = new GetMemberProfileQuery(userId.Value);
        var result = await _sender.Send(query, cancellationToken);
        
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

        var command = new UpdateMemberProfileCommand(
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
            request.Bio);

        var result = await _sender.Send(command, cancellationToken);
        
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
        var query = new GetMemberProfileQuery(userId);
        var result = await _sender.Send(query, cancellationToken);
        
        return Ok(result);
    }
}
