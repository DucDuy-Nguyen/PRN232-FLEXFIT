using System;
using MediatR;

namespace FlexFit.Identity.Application.Authentication.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string CurrentAccessToken) : IRequest<ChangePasswordResponse>;

public sealed record ChangePasswordResponse(
    string Message);
