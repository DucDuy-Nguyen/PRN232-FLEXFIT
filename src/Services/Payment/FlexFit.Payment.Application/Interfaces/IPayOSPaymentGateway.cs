using System.Threading.Tasks;

namespace FlexFit.Payment.Application.Interfaces
{
    public class PayOSPaymentLinkResult
    {
        public string CheckoutUrl { get; set; } = null!;
    }

    public class PayOSWebhookData
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = null!;
    }

    public interface IPayOSPaymentGateway
    {
        Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(long orderCode, int amount, string description, string cancelUrl, string returnUrl);
        Task<PayOSWebhookData?> VerifyWebhookSignatureAsync(object webhookBody);
    }
}
