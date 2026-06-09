using wallet.Models.Requests;
using wallet.Models.Responses;

namespace wallet.Services.PaymentGateway
{
    public interface IPaymentService
    {
        Task<InitPaymentResponse> InitiateDepositAsync(int userId, InitPaymentRequest request, string requestedBy);
        Task<GatewayCallbackResponse> ProcessGatewayCallbackAsync(GatewayWebhookRequest request);
    }
}
