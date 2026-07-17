using MediatR;

namespace FlexFit.Identity.Application.Authentication.Logout;

public sealed record LogoutCommand(
    string AccessToken,
    string RefreshToken) : IRequest<LogoutResponse>;

public sealed record LogoutResponse(
    string Message);
