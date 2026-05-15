namespace Flexfit.Service
{
    using Flexfit.DTOs;
    using Flexfit.Helpers;
    using Flexfit.Models;
    using Flexfit.Repositories;
    using Google.Apis.Auth;
    using Microsoft.Extensions.Configuration;

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwt;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository userRepository, JwtHelper jwt, IConfiguration config)
        {
            _userRepository = userRepository;
            _jwt = jwt;
            _config = config;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.GetByEmailAsync(request.Email) != null)
                throw new Exception("Email đã tồn tại");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = PasswordHasher.Hash(request.Password)
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null) throw new Exception("Email không tồn tại");

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                throw new Exception("Mật khẩu không đúng");

            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }

        public async Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { _config["Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                var user = await _userRepository.GetByEmailAsync(payload.Email);

                if (user == null)
                {
                    user = new User
                    {
                        UserId = Guid.NewGuid(),
                        FullName = payload.Name,
                        Email = payload.Email,
                        PasswordHash = "" // Google login doesn't need password hash
                    };
                    await _userRepository.AddAsync(user);
                    await _userRepository.SaveChangesAsync();
                }

                return new AuthResponse
                {
                    Token = _jwt.GenerateToken(user.UserId, user.Email),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Xác thực Google thất bại: " + ex.Message);
            }
        }
    }
}
