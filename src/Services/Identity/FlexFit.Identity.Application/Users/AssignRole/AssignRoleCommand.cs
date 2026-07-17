using System;
using MediatR;

namespace FlexFit.Identity.Application.Users.AssignRole;

/// <summary>
/// Assigns a role to a target user.
/// ActorUserId is taken from the authenticated JWT — never from the request body.
/// </summary>
public sealed record AssignRoleCommand(
    Guid TargetUserId,
    string RoleName,
    Guid ActorUserId) : IRequest<Unit>;
