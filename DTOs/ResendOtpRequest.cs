namespace Flexfit.DTOs
{
    public class ResendOtpRequest
    {
        public string Email { get; set; } = string.Empty;

        // Nhận giá trị: "VERIFY_EMAIL" hoặc "FORGOT_PASSWORD"
        public string Reason { get; set; } = string.Empty;
    }
}