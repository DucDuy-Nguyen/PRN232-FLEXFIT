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
        private readonly IEmailService _emailService; // Thêm EmailService


        public AuthService(IUserRepository userRepository, JwtHelper jwt, IConfiguration config, IEmailService emailService)
        {
            _userRepository = userRepository;
            _jwt = jwt;
            _config = config;
            _emailService = emailService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // 1. Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new Exception("Số điện thoại không được để trống");

            if (await _userRepository.ExistsByEmailAsync(request.Email))
                throw new Exception("Email đã tồn tại");

            // 2. TẠO MÃ OTP TRƯỚC
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // 3. Khởi tạo đối tượng User và gán luôn OTP vào
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = PasswordHasher.Hash(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,

                // --- PHẦN VERIFY EMAIL ---
                IsEmailVerified = false,
                EmailVerificationToken = otpCode, // Gán trực tiếp mã 6 số
                VerificationTokenExpires = DateTime.UtcNow.AddMinutes(2) // Hẹn giờ 2 phút
            };

            // 4. LƯU XUỐNG DATABASE (CHỈ LƯU 1 LẦN DUY NHẤT)
            await _userRepository.AddAsync(user);
            // Lưu ý: Nếu hàm AddAsync của bạn chưa có SaveChanges, thì cần gọi hàm dưới đây:
            await _userRepository.SaveChangesAsync();

            // 5. GỬI EMAIL XÁC THỰC
            var subject = "Mã xác thực tài khoản FlexFit";
            var body = $@"
      <h3>Chào bạn,</h3>
       <p>Mã xác thực (OTP) để kích hoạt tài khoản của bạn là:</p>
       <h2 style='color: #2e6c80; letter-spacing: 5px;'>{otpCode}</h2>
        <p><i>Mã này sẽ hết hiệu lực sau 2 phút. Vui lòng không chia sẻ mã này cho người khác.</i></p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            // 6. Trả về Token đăng nhập tạm thời (nếu có)
            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }

        // Hàm xử lý khi người dùng click vào link xác thực
        public async Task<string> VerifyEmailAsync(string email, string otpCode)
        {
            // 1. Tìm user theo email
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null || user.EmailVerificationToken != otpCode)
            {
                return "Mã xác thực không hợp lệ hoặc sai email.";
            }

            // 2. Kiểm tra xem mã còn hạn trong 2 phút không?
            if (user.VerificationTokenExpires < DateTime.UtcNow)
            {
                return "Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi mã mới.";
            }

            // 3. Nếu đúng hết -> Cho phép kích hoạt tài khoản
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null; // Xóa mã đi
            user.VerificationTokenExpires = null;

            await _userRepository.UpdateAsync(user);

            return "Xác thực tài khoản thành công!";
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
