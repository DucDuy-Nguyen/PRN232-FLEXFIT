using System;
using System.Text.Json;
using System.Threading.Tasks;
using FlexFit.Payment.API.Interfaces.Gateways;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace FlexFit.Payment.API.Gateways.PayOS
{
    public class PayOSPaymentGateway : IPayOSPaymentGateway
    {
        private readonly PayOSClient _payOSClient;

        public PayOSPaymentGateway(PayOSClient payOSClient)
        {
            _payOSClient = payOSClient;
        }

        public async Task<PayOSPaymentLinkResult> CreatePaymentLinkAsync(long orderCode, int amount, string description, string cancelUrl, string returnUrl)
        {
            var truncatedDescription = description.Substring(0, Math.Min(25, description.Length));
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = truncatedDescription,
                CancelUrl = cancelUrl,
                ReturnUrl = returnUrl
            };

            var result = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);

            return new PayOSPaymentLinkResult
            {
                CheckoutUrl = result.CheckoutUrl
            };
        }

        public async Task<PayOSWebhookData?> VerifyWebhookSignatureAsync(object webhookBody)
        {
            Webhook? body = null;

            if (webhookBody is Webhook casted)
            {
                body = casted;
            }
            else if (webhookBody is JsonElement element)
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                body = JsonSerializer.Deserialize<Webhook>(element.GetRawText(), options);
            }

            if (body == null)
            {
                throw new ArgumentException("Dữ liệu webhook không hợp lệ.");
            }

            var verifiedData = await _payOSClient.Webhooks.VerifyAsync(body);
            if (verifiedData == null)
            {
                return null;
            }

            return new PayOSWebhookData
            {
                OrderCode = verifiedData.OrderCode,
                Amount = (int)verifiedData.Amount,
                Description = verifiedData.Description
            };
        }
    }
}
