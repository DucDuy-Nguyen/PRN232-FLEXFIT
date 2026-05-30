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
        private readonly ISystemLogService _systemLogService;


        public AuthService(IUserRepository userRepository, JwtHelper jwt, IConfiguration config, IEmailService emailService, ISystemLogService systemLogService)
        {
            _userRepository = userRepository;
            _jwt = jwt;
            _config = config;
            _emailService = emailService;
            _systemLogService = systemLogService;
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
                CreatedAt = DateTimeHelper.GetVietnamTime(),
                IsActive = true,

                // --- PHẦN VERIFY EMAIL ---
                IsEmailVerified = false,
                EmailVerificationToken = otpCode, // Gán trực tiếp mã 6 số
                VerificationTokenExpires = DateTimeHelper.GetVietnamTime().AddMinutes(2), // Hẹn giờ 2 phút

                // Khởi tạo ví tín dụng ban đầu với số dư = 0
                UserCredit = new UserCredit
                {
                    UserCreditId = Guid.NewGuid(),
                    Balance = 0,
                    TotalEarned = 0,
                    TotalSpent = 0,
                    UpdatedAt = DateTimeHelper.GetVietnamTime()
                }
            };

            // 4. LƯU XUỐNG DATABASE (CHỈ LƯU 1 LẦN DUY NHẤT)
            await _userRepository.AddAsync(user);
            // Lưu ý: Nếu hàm AddAsync của bạn chưa có SaveChanges, thì cần gọi hàm dưới đây:
            await _userRepository.SaveChangesAsync();
            await _systemLogService.LogActionAsync(user.UserId, "REGISTER", $"Đăng ký tài khoản mới: {user.Email}", null);

            // 5. GỬI EMAIL XÁC THỰC
            var subject = "Mã xác thực tài khoản FlexFit";
            var body = $@"
      <h3>Chào {user.FullName},</h3>
       <p>Mã xác thực (OTP) để kích hoạt tài khoản của bạn là:</p>
       <h2 style='color: #2e6c80; letter-spacing: 5px;'>{otpCode}</h2>
        <p><i>Mã này sẽ hết hiệu lực sau 2 phút. Vui lòng không chia sẻ mã này cho người khác.</i></p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            // 6. Trả về Token đăng nhập tạm thời (nếu có)
            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email, new List<string>()), // Đăng ký xong thường chưa có Role
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }

        // Hàm xử lý khi người dùng click vào link xác thực
        public async Task<string> VerifyEmailAsync(string email, string otpCode)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null || user.EmailVerificationToken != otpCode)
            {
                return "Mã xác thực không hợp lệ hoặc sai email.";
            }

            if (user.VerificationTokenExpires < DateTimeHelper.GetVietnamTime())
            {
                return "Mã xác thực đã hết hạn. Vui lòng yêu cầu gửi mã mới.";
            }

            // 1. Kích hoạt trạng thái tài khoản
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.VerificationTokenExpires = null;

            // 2. TỰ ĐỘNG CẤP QUYỀN ROLE: "Member"
            // Kiểm tra xem User đã có danh sách Roles chưa để tránh lỗi NullReferenceException
            if (user.UserRoles == null)
            {
                user.UserRoles = new List<UserRole>();
            }

            // Đảm bảo không add trùng nếu tài khoản đã lỡ có quyền Member trước đó
            if (!user.UserRoles.Any(ur => ur.Role?.RoleName == "Member"))
            {
                // Bạn cần chắc chắn trong DB bảng Roles đã có sẵn dòng dữ liệu "Member" nhé.
                // Đoạn này lấy ra RoleId của Role "Member" từ DB hoặc nếu DB cấu hình cứng bằng cách nạp trực tiếp qua Repository.
                // Ở đây tôi giả định cấu hình thực thể của bạn là thực thể trung gian UserRole(UserId, RoleId).

                var memberRole = await _userRepository.GetRoleByNameAsync("Member");
                if (memberRole != null)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = memberRole.RoleId
                    });
                }
            }

            // 3. Cập nhật thực thể đã nạp quyền mới xuống DB
            await _userRepository.UpdateAsync(user);
            await _systemLogService.LogActionAsync(user.UserId, "VERIFY_EMAIL", $"Xác thực email tài khoản thành công: {email}", null);

            return "Xác thực tài khoản và kích hoạt quyền Hội viên thành công!";
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null) throw new Exception("Email không tồn tại");

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                throw new Exception("Mật khẩu không đúng");

            var roles = user.UserRoles?.Select(ur => ur.Role.RoleName).ToList() ?? new List<string>();

            await _systemLogService.LogActionAsync(user.UserId, "LOGIN", $"Đăng nhập hệ thống thành công: {user.Email}", null);

            return new AuthResponse
            {
                Token = _jwt.GenerateToken(user.UserId, user.Email, roles),
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
                        PasswordHash = "", // Google login doesn't need password hash
                        IsActive = true,
                        IsEmailVerified = true,
                        CreatedAt = DateTimeHelper.GetVietnamTime(),
                        UserCredit = new UserCredit
                        {
                            UserCreditId = Guid.NewGuid(),
                            Balance = 0,
                            TotalEarned = 0,
                            TotalSpent = 0,
                            UpdatedAt = DateTimeHelper.GetVietnamTime()
                        }
                    };
                    await _userRepository.AddAsync(user);
                    await _userRepository.SaveChangesAsync();
                }

                // Tự động cấp quyền Member nếu chưa có
                if (user.UserRoles == null)
                {
                    user.UserRoles = new List<UserRole>();
                }

                if (!user.UserRoles.Any(ur => ur.Role?.RoleName == "Member"))
                {
                    var memberRole = await _userRepository.GetRoleByNameAsync("Member");
                    if (memberRole != null)
                    {
                        user.UserRoles.Add(new UserRole
                        {
                            UserId = user.UserId,
                            RoleId = memberRole.RoleId,
                            AssignedAt = DateTimeHelper.GetVietnamTime()
                        });
                        await _userRepository.UpdateAsync(user);
                        
                        // Nạp lại thông tin User kèm Roles đầy đủ
                        user = await _userRepository.GetByEmailAsync(user.Email);
                    }
                }

                var roles = user.UserRoles?.Select(ur => ur.Role.RoleName).ToList() ?? new List<string>();

                await _systemLogService.LogActionAsync(user.UserId, "LOGIN_GOOGLE", $"Đăng nhập bằng tài khoản Google thành công: {user.Email}", null);

                return new AuthResponse
                {
                    Token = _jwt.GenerateToken(user.UserId, user.Email, roles),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Xác thực Google thất bại: " + ex.Message);
            }
        }
        // --- 1. LOGIC GỬI MÃ OTP QUÊN MẬT KHẨU ---
        public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            // Kiểm tra email xem có tồn tại trong hệ thống không
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new Exception("Email này không tồn tại trên hệ thống.");

            // Tạo mã OTP ngẫu nhiên gồm 6 chữ số
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Lưu OTP tạm thời vào tài khoản User và đặt hạn 3 phút
            user.EmailVerificationToken = otpCode;
            user.VerificationTokenExpires = DateTimeHelper.GetVietnamTime().AddMinutes(3);

            await _userRepository.UpdateAsync(user);
            await _systemLogService.LogActionAsync(user.UserId, "FORGOT_PASSWORD", $"Yêu cầu gửi OTP khôi phục mật khẩu cho email: {user.Email}", null);

            // Tiến hành gửi Email chứa OTP cho người dùng
            var subject = "Mã OTP khôi phục mật khẩu FlexFit";
            var body = $@"
                <h3>Chào {user.FullName},</h3>
                <p>Bạn đã yêu cầu đặt lại mật khẩu tại FlexFit. Mã OTP xác thực của bạn là:</p>
                <h2 style='color: #d9534f; letter-spacing: 5px;'>{otpCode}</h2>
                <p><i>Mã này sẽ hết hiệu lực sau 3 phút. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</i></p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return "Mã OTP khôi phục mật khẩu đã được gửi đến Email của bạn.";
        }

        // --- 2. LOGIC XÁC THỰC OTP VÀ ĐỔI MẬT KHẨU MỚI ---
        public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new Exception("Email không tồn tại.");

            // Kiểm tra mã OTP gửi lên xem có khớp không
            if (user.EmailVerificationToken != request.OtpCode)
                throw new Exception("Mã OTP xác thực không chính xác.");

            // Kiểm tra mã OTP còn hạn sử dụng không
            if (user.VerificationTokenExpires < DateTimeHelper.GetVietnamTime())
                throw new Exception("Mã OTP đã hết hạn sử dụng. Vui lòng lấy mã mới.");

            // Tiến hành mã hóa (Hash) mật khẩu mới và cập nhật cho User
            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);

            // Dọn dẹp sạch mã OTP sau khi đổi thành công để bảo mật
            user.EmailVerificationToken = null;
            user.VerificationTokenExpires = null;

            await _userRepository.UpdateAsync(user);
            await _systemLogService.LogActionAsync(user.UserId, "RESET_PASSWORD", $"Đặt lại mật khẩu thành công cho email: {user.Email}", null);

            return "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";

        }
        // Resend OTP cho cả 2 trường hợp: Xác thực email và Quên mật khẩu
        public async Task<string> ResendOtpAsync(ResendOtpRequest request)
        {
            // 1. Kiểm tra Email có tồn tại trong hệ thống không
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new Exception("Email không tồn tại trong hệ thống.");

            // 2. Tạo mã OTP 6 số mới hoàn toàn
            var random = new Random();
            var newOtpCode = random.Next(100000, 999999).ToString();

            string subject = "";
            string body = "";

            // 3. Phân nhánh xử lý theo lý do (Reason) gửi lại mã
            switch (request.Reason.ToUpper())
            {
                case "VERIFY_EMAIL":
                    if (user.IsEmailVerified)
                        throw new Exception("Tài khoản của bạn đã được xác thực trước đó rồi.");

                    // Reset mã mới và đặt lại hạn 2 phút
                    user.EmailVerificationToken = newOtpCode;
                    user.VerificationTokenExpires = DateTime.UtcNow.AddMinutes(2);

                    subject = "Gửi lại: Mã xác thực tài khoản FlexFit";
                    body = $@"
                <h3>Chào {user.FullName},</h3>
                <p>Bạn đã yêu cầu gửi lại mã xác thực tài khoản. Mã OTP mới của bạn là:</p>
                <h2 style='color: #2e6c80; letter-spacing: 5px;'>{newOtpCode}</h2>
                <p><i>Mã này sẽ hết hiệu lực sau 2 phút. Vui lòng không chia sẻ mã này.</i></p>";
                    break;

                case "FORGOT_PASSWORD":
                    // Reset mã mới và đặt lại hạn 3 phút cho quên mật khẩu
                    user.EmailVerificationToken = newOtpCode;
                    user.VerificationTokenExpires = DateTime.UtcNow.AddMinutes(3);

                    subject = "Gửi lại: Mã OTP khôi phục mật khẩu FlexFit";
                    body = $@"
                <h3>Chào {user.FullName},</h3>
                <p>Bạn đã yêu cầu gửi lại mã khôi phục mật khẩu. Mã OTP mới của bạn là:</p>
                <h2 style='color: #d9534f; letter-spacing: 5px;'>{newOtpCode}</h2>
                <p><i>Mã này sẽ hết hiệu lực sau 3 minutes. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</i></p>";
                    break;

                default:
                    throw new ArgumentException("Lý do gửi lại OTP (Reason) không hợp lệ. Chỉ chấp nhận 'VERIFY_EMAIL' hoặc 'FORGOT_PASSWORD'.");
            }

            // 4. Cập nhật mã OTP mới lưu xuống Database
            await _userRepository.UpdateAsync(user);

            // 5. Tiến hành bắn Email
            await _emailService.SendEmailAsync(user.Email, subject, body);

            return "Mã OTP mới đã được gửi lại vào Email của bạn thành công!";
        }
    }
}