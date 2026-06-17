using Microsoft.AspNetCore.Mvc;
using wallet.Services.PaymentGateway;
using wallet.Services;
using wallet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using wallet.Helpers;
using wallet.Models.Requests;
using wallet.Models.Responses;
using wallet.Exceptions;

namespace wallet.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/payment")]
    [ApiVersion("1.0")]
    public class PaymentController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly IPaymentService _paymentService;
        private readonly WalletdbContext _context;
        private readonly string _gatewaySecretKey;
        private const string FrontendBaseUrl = "https://myfrontend.com/payment";
        

        public PaymentController(IWalletService walletService,IPaymentService paymentServicee,WalletdbContext context, IConfiguration configuration)
        {
            _paymentService = paymentServicee;
            _context = context;
            _walletService = walletService;
            _gatewaySecretKey = configuration["PaymentGateway:SecretKey"]
                ?? throw new InvalidOperationException("PaymentGateway:SecretKey is not configured.");
        }

        /// <summary>
        /// Handles the payment gateway redirection/notification and returns the target frontend URL based on the payment result.
        /// </summary>
        /// <param name="request">The query parameters sent by the payment gateway containing transaction status and signature.</param>
        /// <returns>A target redirect URL with corresponding status parameters.</returns>
        [HttpGet("payment-notify")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GatewayNotifyResponse), StatusCodes.Status200OK)] 
        public async Task<ActionResult<GatewayNotifyResponse>> PaymentNotify([FromQuery] GatewayNotifyRequest request)
        {
            //  Security Validation 
            bool isSecure = HashHelper.VerifySignature(
                request.MerchantOrderId,
                request.Amount,
                request.StatusCode,
                _gatewaySecretKey,
                request.HashSign
            );

            if (!isSecure)
            {
                return Ok(new GatewayNotifyResponse
                {
                    IsSuccess = false,
                    TargetUrl = $"{FrontendBaseUrl}/fail?error=security_violation"                
                });
                //return Redirect($"{FrontendBaseUrl}/fail?error=security_violation");
            }
         
            if (!Guid.TryParse(request.MerchantOrderId, out Guid transactionId))
            {
                return Ok(new GatewayNotifyResponse
                {
                    IsSuccess = false,
                    TargetUrl = $"{FrontendBaseUrl}/fail?error=invalid_order_id"                 
                });
            }

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null)
            {
                return Ok(new GatewayNotifyResponse
                {
                    IsSuccess = false,
                    TargetUrl = $"{FrontendBaseUrl}/fail?error=transaction_not_found"                   
                });
            }
            else
            {
                if (request.StatusCode == "0000") // 0000 is defined as the payment success status code.
                {
                    return Ok(new GatewayNotifyResponse
                    {
                        IsSuccess = true,
                        TargetUrl = $"{FrontendBaseUrl}/success?txnId={transaction.TransactionId}&amount={request.Amount}",                    
                    });
                }
                else
                {
                    string reason = string.IsNullOrEmpty(request.Message) ? "payment_failed" : request.Message;
                    return Ok(new GatewayNotifyResponse
                    {
                        IsSuccess = false,
                        TargetUrl = $"{FrontendBaseUrl}/fail?txnId={transaction.TransactionId}&reason={reason}"                       
                    });
                }
            }
        }


        /// <summary>
        /// Processes the asynchronous webhook callback from the payment gateway to settle the transaction and update the wallet balance.
        /// </summary>
        /// <param name="request">The webhook payload containing transaction status, amounts, and secure hash signature.</param>
        /// <returns>The settlement result, updated wallet number, and current balance.</returns>
        [HttpPost("payment-confirm")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<GatewayCallbackResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]    
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]       
        public async Task<ActionResult<ApiResponse<GatewayCallbackResponse>>> GatewayCallback(
        [FromBody] GatewayWebhookRequest request)
        {          
            //  Security Validation 
            bool isSecure = HashHelper.VerifySignature(
                request.MerchantOrderId,
                request.Amount,
                request.StatusCode,
                _gatewaySecretKey,
                request.HashSign
            );

            if (!isSecure)
            {
                throw new AppException("Invalid gateway signature.", StatusCodes.Status400BadRequest);
            }
            var result = await _paymentService.ProcessGatewayCallbackAsync(request);

            return Ok(ApiResponse<GatewayCallbackResponse>.Ok(result, "Transaction settled successfully."));
        }

    }
}
