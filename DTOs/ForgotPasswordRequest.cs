namespace Flexfit.DTOs
{
    // DTO nhận email từ người dùng khi bấm quên mật khẩu
    public class ForgotPasswordRequest
    {
        public required string Email { get; set; }
    }

    // DTO nhận thông tin đổi mật khẩu mới kèm OTP xác thực
    public class ResetPasswordRequest
    {
        public required string Email { get; set; }
        public required string OtpCode { get; set; }
        public required string NewPassword { get; set; }
    }
}