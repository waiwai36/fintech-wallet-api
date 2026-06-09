using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using wallet.Data.Entities;
using wallet.Models.Requests;
using wallet.Models.Responses;
using wallet.Services;
using wallet.Services.PaymentGateway;

namespace wallet.Controllers;

[Authorize(Roles = "User")]
[Route("api/v{version:apiVersion}/wallet")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
public class WalletController : BaseApiController
{
    private readonly IWalletService _walletService;
    private readonly IPaymentService _paymentService;

    public WalletController(IWalletService walletService,IPaymentService paymentService)
    {
        _walletService = walletService;
        _paymentService = paymentService;
    }

    /// <summary>
    /// Initiates a manual bank transfer deposit request.
    /// </summary>
    /// <param name="request">The manual bank deposit transaction details.</param>
    /// <returns>A pending deposit details waiting for admin review.</returns>
    [HttpPost("deposit/bank-transfer")]
    [Authorize(Policy = "Deposit")]
    [ProducesResponseType(typeof(ApiResponse<DepositResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)] 
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]   
    public async Task<ActionResult<ApiResponse<DepositResponse>>> InitiateBankDeposit([FromBody] DepositRequest request)
    {      
        var result = await _walletService.InitiateBankDepositAsync(CurrentUserId, request, CurrentUserName);
        return SuccessResponse(result, "Deposit request submitted successfully. Waiting for admin approval.");
       
    }

    /// <summary>
    /// Initiates an online payment via third-party gateway.
    /// </summary>
    /// <param name="request">The currency amount and payment method details.</param>
    /// <returns>A fake or real transaction redirect URL and internal order tracker ID.</returns>
    [HttpPost("deposit/gateway")]
    [Authorize(Policy = "Deposit")]
    [ProducesResponseType(typeof(ApiResponse<InitPaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)] 
    public async Task<ActionResult<ApiResponse<InitPaymentResponse>>> InitiateGatewayDeposit([FromBody] InitPaymentRequest request)
    {     
        var result = await _paymentService.InitiateDepositAsync(CurrentUserId, request, CurrentUserName);
        return SuccessResponse(result, "Payment initiated. Please complete the process via the payment URL.");       
    }

    /// <summary>
    /// Withdraws a specified amount from user's current balance.
    /// </summary>
    /// <param name="request">The amount and specific target detail info.</param>
    /// <returns>The remaining balance info along with confirmation reference status.</returns>
    [HttpPost("withdraw")]
    [Authorize(Policy = "CashOut_Withdraw")]
    [ProducesResponseType(typeof(ApiResponse<WithDrawResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)] 
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]   
    public async Task<ActionResult<ApiResponse<WithDrawResponse>>> Withdraw([FromBody] WithdrawRequest request)
    {       
        var result = await _walletService.WithdrawAsync(CurrentUserId, request, CurrentUserName!);
        return SuccessResponse(result, "Withdrawal successful.");       
    }

    /// <summary>
    /// Transfers money from user's wallet into another user's active wallet.
    /// </summary>
    /// <param name="request">The amount and receiver wallet number identifier.</param>
    /// <returns>The confirmation code context.</returns>
    [HttpPost("transfer")]
    [Authorize(Policy = "Transfer")]
    [ProducesResponseType(typeof(ApiResponse<TransferResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)] 
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]  
    public async Task<ActionResult<ApiResponse<TransferResponse>>> Transfer([FromBody] TransferRequest request)
    {     
        var result = await _walletService.TransferAsync(CurrentUserId, request, CurrentUserName);
        return SuccessResponse(result, "Transfer successful.");       
    }

    /// <summary>
    /// Retrieves full history ledger lines based on given filter ranges.
    /// </summary>
    /// <param name="filter">The query pagination indexes and date ranges filtering payload.</param>
    /// <returns>Paged collection payload along with summary context header info.</returns>
    [HttpGet("bank-statement")]
    [Authorize(Policy = "Statement")]
    [ProducesResponseType(typeof(ApiResponse<BankStatementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)] 
    public async Task<ActionResult<ApiResponse<BankStatementResponse>>> GetBankStatement(
        [FromQuery] TransactionFilterRequest filter)
    {
       
        var statement = await _walletService.GetBankStatementAsync(CurrentUserId, filter);
        return SuccessResponse(statement, "Bank statement generated successfully.");        
    }
  

    /// <summary>
    /// Fetches the profile limits and active balance info regarding current user context.
    /// </summary>
    /// <returns>The configuration limit fields details.</returns>
    [HttpGet("wallet")]
    [Authorize(Policy = "Statement")]
    [ProducesResponseType(typeof(ApiResponse<WalletDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WalletDetailsResponse>>> GetWalletDetails()
    {     
        var walletDetails = await _walletService.GetWalletDetailsAsync(CurrentUserId);
        return SuccessResponse(walletDetails, "Wallet details retrieved successfully.");     
    }
}