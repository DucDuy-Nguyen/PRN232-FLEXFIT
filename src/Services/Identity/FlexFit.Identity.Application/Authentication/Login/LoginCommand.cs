using MediatR;

namespace FlexFit.Identity.Application.Authentication.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResponse>;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType = "Bearer");
