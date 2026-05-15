namespace Flexfit.Service
{
    using Flexfit.DTOs;
    using Flexfit.Helpers;
    using Flexfit.Models;
    using Microsoft.EntityFrameworkCore;

    public class AuthService
    {
        private readonly FlexFitDbContext _db;
        private readonly JwtHelper _jwt;

        public AuthService(FlexFitDbContext db, JwtHelper jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email))
                throw new Exception("Email đã tồn tại");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = PasswordHasher.Hash(request.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
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
