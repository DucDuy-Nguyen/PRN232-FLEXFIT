using System;
using MediatR;

namespace FlexFit.Identity.Application.Users.ChangeUserStatus;

/// <summary>
/// Changes the status (active/inactive) of a target user.
/// ActorUserId is injected from the authenticated JWT.
/// </summary>
public sealed record ChangeUserStatusCommand(
    Guid TargetUserId,
    bool IsActive,
    Guid ActorUserId) : IRequest<Unit>;
