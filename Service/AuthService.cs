using Flexfit.DTOs;
using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;

namespace Flexfit.Service
{
    public class AuthService
    {
        private readonly IUserRepository _userRepo; // Dùng Repository thay vì DbContext
        private readonly JwtHelper _jwt;

        public AuthService(IUserRepository userRepo, JwtHelper jwt)
        {
            _userRepo = userRepo;
            _jwt = jwt;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // 1. Kiểm tra không được để trống số điện thoại
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new Exception("Số điện thoại không được để trống");

            if (await _userRepo.ExistsByEmailAsync(request.Email))
                throw new Exception("Email đã tồn tại");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                // 2. Gán số điện thoại từ request vào model
               
                PasswordHash = PasswordHasher.Hash(request.Password),
                PhoneNumber = request.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null) throw new Exception("Email không tồn tại");

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                throw new Exception("Mật khẩu không đúng");

            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }
    }
}