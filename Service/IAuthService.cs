using Flexfit.DTOs;

namespace Flexfit.Service
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request);
        Task<string> VerifyEmailAsync(string email, string otpCode);
    }
}
