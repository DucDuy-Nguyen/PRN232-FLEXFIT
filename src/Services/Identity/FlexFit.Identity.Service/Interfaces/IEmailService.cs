using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.Service.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
    Task SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, string purpose, CancellationToken cancellationToken = default);
}
