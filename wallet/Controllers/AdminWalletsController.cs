using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using wallet.Models.Requests;
using wallet.Models.Responses;
using wallet.Services;

namespace wallet.Controllers;
/// <summary>
/// Financial and operational management endpoints for Administrator use only.
/// </summary>
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/admin")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
public class AdminWalletsController : BaseApiController
{
    private readonly IWalletService _walletService;
    public AdminWalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>
    /// Lock or unlock a specific user wallet.
    /// </summary>
    /// <response code="200">Wallet lock state successfully updated.</response>
    /// <response code="400">Invalid request </response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="403">Forbidden access</response>
    /// <response code="404">Specified wallet account was not found.</response>
    [HttpPost("wallets/{id}/lock")]
    [ProducesResponseType(typeof(ApiResponse<WalletLockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)] 
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
   
    public async Task<ActionResult<ApiResponse<WalletLockResponse>>> LockOrUnlockWallet(
        int id,
        [FromBody] LockWalletRequest request)
    {     
        var result = await _walletService.SetWalletLockStateAsync(id, request.IsLocked, CurrentUserName);

        var message = request.IsLocked ? "Wallet locked successfully." : "Wallet unlocked successfully.";
      
        return SuccessResponse(result, message);
    }

    /// <summary>
    /// Manually credit or debit a specified wallet balance.
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/admin/wallet/adjust-balance
    ///     {
    ///        "walletNumber": "W-20260608-ABC123",
    ///        "action": "Credit", // or "Debit"
    ///        "amount": 50000,
    ///        "remark": "Manual correction by system admin"
    ///     }
    /// </remarks>
    /// <response code="200">Balance adjusted successfully.</response>
    /// <response code="400">Invalid adjustment parameters or insufficient funds on debit action.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="403">Forbidden access.</response>
    /// <response code="404">Target wallet number not found.</response>
    [HttpPost("wallet/adjust-balance")]
    [ProducesResponseType(typeof(ApiResponse<WalletLockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AdjustBalanceResponse>>> AdjustBalance([FromBody] AdjustBalanceRequest request)
    {
        var result = await _walletService.AdjustBalanceAsync(request,CurrentUserName);
        return SuccessResponse(result, "Adjustment successful.");       
    }

    /// <summary>
    /// Approve a pending offline manual bank deposit.
    /// </summary>
    /// <response code="200">Success</response>
    /// <response code="400">Wallet account associated with the transaction is locked or inactive.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="403">Forbidden access.</response>
    /// <response code="404">Pending bank deposit reference record not found.</response>
    [HttpPost("deposit/approve")]
    [ProducesResponseType(typeof(ApiResponse<WalletLockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DepositResponse>>> ApproveBankDeposit([FromBody] ApproveDepositRequest request)
    {      
        var result = await _walletService.ApproveBankDepositAsync(request, CurrentUserName);
        return SuccessResponse(result, "Bank deposit approved and wallet balance updated successfully.");       
    }
}
