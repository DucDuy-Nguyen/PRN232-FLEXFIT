using FlexFit.Identity.Service.Configurations;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using FlexFit.Identity.Service.Interfaces;

namespace FlexFit.Identity.API.Infrastructure.Email;

public sealed class MailKitEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IOptions<EmailOptions> options, ILogger<MailKitEmailService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("SMTP host is required.");
        if (string.IsNullOrWhiteSpace(_options.Username))
            throw new InvalidOperationException("SMTP username is required.");
        if (string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("SMTP password is required.");
        if (string.IsNullOrWhiteSpace(_options.SenderEmail))
            throw new InvalidOperationException("SMTP sender email is required.");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {Recipient}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", toEmail);
            throw;
        }
    }

    public async Task SendOtpEmailAsync(
        string toEmail, 
        string recipientName, 
        string otpCode, 
        string purpose, 
        CancellationToken cancellationToken = default)
    {
        var isVerify = purpose.Equals("EmailVerification", StringComparison.OrdinalIgnoreCase) || 
                       purpose.Equals("VerifyEmail", StringComparison.OrdinalIgnoreCase);
        
        var subject = isVerify 
            ? "FlexFit - Xác thực tài khoản của bạn" 
            : "FlexFit - Đặt lại mật khẩu";

        var title = isVerify
            ? "Xác thực địa chỉ Email của bạn"
            : "Yêu cầu khôi phục mật khẩu";

        var introduction = isVerify
            ? "Cảm ơn bạn đã đăng ký tài khoản tại FlexFit. Vui lòng sử dụng mã OTP dưới đây để hoàn tất việc xác thực địa chỉ email:"
            : "Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn. Vui lòng sử dụng mã OTP dưới đây để đặt lại mật khẩu của mình:";

        var htmlBody = $@"
            <div style=""font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px; background-color: #ffffff;"">
                <div style=""text-align: center; margin-bottom: 24px;"">
                    <h2 style=""color: #3b82f6; margin: 0; font-size: 24px; font-weight: 700;"">FLEXFIT</h2>
                </div>
                <div style=""color: #333333; font-size: 16px; line-height: 1.5; margin-bottom: 20px;"">
                    <p>Xin chào <strong>{recipientName}</strong>,</p>
                    <p>{introduction}</p>
                </div>
                <div style=""text-align: center; margin: 30px 0;"">
                    <span style=""display: inline-block; font-family: monospace; font-size: 32px; font-weight: bold; letter-spacing: 4px; color: #1e3a8a; background-color: #eff6ff; padding: 12px 24px; border-radius: 6px; border: 1px dashed #bfdbfe;"">
                        {otpCode}
                    </span>
                </div>
                <div style=""color: #666666; font-size: 14px; line-height: 1.5; margin-bottom: 24px;"">
                    <p>Mã này có hiệu lực trong vòng <strong>5 phút</strong>. Tuyệt đối không chia sẻ mã này với bất kỳ ai để đảm bảo an toàn bảo mật.</p>
                    <p>Nếu bạn không gửi yêu cầu này, vui lòng bỏ qua email này.</p>
                </div>
                <hr style=""border: 0; border-top: 1px solid #eeeeee; margin-bottom: 20px;"" />
                <div style=""text-align: center; color: #999999; font-size: 12px;"">
                    <p>&copy; {DateTime.UtcNow.Year} FlexFit Corporation. All rights reserved.</p>
                </div>
            </div>";

        _logger.LogInformation("Initiating OTP email delivery for {Recipient} (Purpose: {Purpose})", toEmail, purpose);

        await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }
}
