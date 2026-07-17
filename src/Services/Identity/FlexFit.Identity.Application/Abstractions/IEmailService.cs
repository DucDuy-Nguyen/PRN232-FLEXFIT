namespace FlexFit.Identity.Application.Abstractions;

/// <summary>
/// Email service abstraction — defined in Application, implemented in Infrastructure using MailKit.
/// Preserves the same email sending capability as the monolith's EmailService.
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);

    Task SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, string purpose, CancellationToken cancellationToken = default);
}
