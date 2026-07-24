using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Service.DTOs.Contracts.Authentication;

namespace FlexFit.Identity.Service.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(
        string fullName, 
        string email, 
        string password, 
        string? phoneNumber, 
        CancellationToken cancellationToken = default);

    Task<LoginResponse> LoginAsync(
        string email, 
        string password, 
        CancellationToken cancellationToken = default);

    Task<GoogleLoginResponse> GoogleLoginAsync(
        string idToken, 
        CancellationToken cancellationToken = default);

    Task<LoginResponse> RefreshTokenAsync(
        string accessToken, 
        string refreshToken, 
        CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(
        string email, 
        string otpCode, 
        CancellationToken cancellationToken = default);

    Task ResendOtpAsync(
        string email, 
        string purpose, 
        CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(
        string email, 
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        string email, 
        string otpCode, 
        string newPassword, 
        string confirmPassword, 
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        string currentAccessToken,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        string accessToken, 
        string refreshToken, 
        CancellationToken cancellationToken = default);
}
