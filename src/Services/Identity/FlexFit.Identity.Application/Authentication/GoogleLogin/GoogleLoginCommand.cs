using System;
using MediatR;

namespace FlexFit.Identity.Application.Authentication.GoogleLogin;

/// <summary>
/// Initiates Google OAuth login using a client-supplied ID token.
/// The ID token is validated server-side against Google's public keys.
/// Client must NOT supply UserId, Role, or EmailVerified — these come from the token.
/// </summary>
public sealed record GoogleLoginCommand(string IdToken) : IRequest<GoogleLoginResponse>;
