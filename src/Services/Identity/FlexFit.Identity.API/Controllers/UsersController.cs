using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FlexFit.Identity.API.Authorization;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Application.Users.Queries;
using FlexFit.Identity.Application.Users.AssignRole;
using FlexFit.Identity.Application.Users.RevokeRole;
using FlexFit.Identity.Application.Users.ChangeUserStatus;
using FlexFit.Identity.API.Contracts.Users;

namespace FlexFit.Identity.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Policy = IdentityPolicies.UserManagement)]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);
        
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedList<UserDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isEmailVerified = null,
        [FromQuery] string? roleName = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery(page, pageSize, search, isActive, isEmailVerified, roleName, sortBy, sortDirection);
        var result = await _sender.Send(query, cancellationToken);
        
        return Ok(result);
    }

    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId ?? Guid.Empty;
        var command = new AssignRoleCommand(userId, request.Role, actorUserId);
        await _sender.Send(command, cancellationToken);
        
        return NoContent();
    }

    [HttpDelete("{userId:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeRole(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId ?? Guid.Empty;
        var command = new RevokeRoleCommand(userId, role, actorUserId);
        await _sender.Send(command, cancellationToken);
        
        return NoContent();
    }

    [HttpPatch("{userId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        Guid userId,
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId ?? Guid.Empty;
        var command = new ChangeUserStatusCommand(userId, request.IsActive, actorUserId);
        await _sender.Send(command, cancellationToken);
        
        return NoContent();
    }
}
