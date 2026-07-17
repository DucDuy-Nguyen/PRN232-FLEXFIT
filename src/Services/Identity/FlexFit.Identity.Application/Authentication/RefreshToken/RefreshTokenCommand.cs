using MediatR;

namespace FlexFit.Identity.Application.Authentication.RefreshToken;

public sealed record RefreshTokenCommand(
    string ExpiredAccessToken,
    string RefreshToken) : IRequest<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType = "Bearer");
