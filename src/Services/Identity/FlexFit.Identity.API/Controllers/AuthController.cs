using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Application.Authentication.ChangePassword;
using FlexFit.Identity.Application.Authentication.ForgotPassword;
using FlexFit.Identity.Application.Authentication.GoogleLogin;
using FlexFit.Identity.Application.Authentication.Login;
using FlexFit.Identity.Application.Authentication.Logout;
using FlexFit.Identity.Application.Authentication.RefreshToken;
using FlexFit.Identity.Application.Authentication.Register;
using FlexFit.Identity.Application.Authentication.ResendOtp;
using FlexFit.Identity.Application.Authentication.ResetPassword;
using FlexFit.Identity.Application.Authentication.VerifyEmail;
using FlexFit.Identity.API.Contracts.Authentication;

namespace FlexFit.Identity.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpPost("google-login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GoogleLoginResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GoogleLoginCommand(request.IdToken);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.ConfirmPassword, request.FullName);
        var result = await _sender.Send(command, cancellationToken);
        
        return CreatedAtAction(null, new { userId = result.UserId, email = result.Email, requiresEmailVerification = true });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            tokenType = result.TokenType
        });
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            tokenType = result.TokenType
        });
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Email, request.Otp);
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new { message = result.Message });
    }

    [HttpPost("resend-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendOtpRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new ResendOtpCommand(request.Email, request.Purpose);
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new { message = result.Message });
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new { message = result.Message });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Email, request.Otp, request.Password, request.ConfirmPassword);
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new { message = result.Message });
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto request, 
        CancellationToken cancellationToken)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        var accessToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
            ? authHeader.Substring(7) 
            : string.Empty;

        var command = new LogoutCommand(accessToken, request.RefreshToken);
        await _sender.Send(command, cancellationToken);
        
        return NoContent();
    }

    [Authorize]
    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var authHeader = Request.Headers.Authorization.ToString();
        var accessToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) 
            ? authHeader.Substring(7) 
            : string.Empty;

        var command = new ChangePasswordCommand(userId.Value, request.CurrentPassword, request.NewPassword, accessToken);
        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new { message = result.Message });
    }
}

public sealed record LogoutRequestDto(string RefreshToken);
