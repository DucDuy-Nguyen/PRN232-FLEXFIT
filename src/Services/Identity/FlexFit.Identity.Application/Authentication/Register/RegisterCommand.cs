using System;
using MediatR;

namespace FlexFit.Identity.Application.Authentication.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FullName) : IRequest<RegisterResponse>;

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string Message);
