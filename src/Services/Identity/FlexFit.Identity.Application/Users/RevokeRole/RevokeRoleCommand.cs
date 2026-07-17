using System;
using MediatR;

namespace FlexFit.Identity.Application.Users.RevokeRole;

/// <summary>
/// Revokes a role from a target user.
/// ActorUserId is injected from the authenticated JWT — never from the request body.
/// </summary>
public sealed record RevokeRoleCommand(
    Guid TargetUserId,
    string RoleName,
    Guid ActorUserId) : IRequest<Unit>;
