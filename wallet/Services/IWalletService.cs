using wallet.Data.Entities;
using wallet.Models.Requests;
using wallet.Models.Responses;

namespace wallet.Services
{
    public interface IWalletService
    {
        Task<DepositResponse> InitiateBankDepositAsync(int userId, DepositRequest request, string requestedBy);
        Task<DepositResponse> ApproveBankDepositAsync(ApproveDepositRequest request, string requestedBy);
        Task<WithDrawResponse> WithdrawAsync(int userId, WithdrawRequest requset, string requestedBy);
        Task<TransferResponse> TransferAsync(int senderUserId, TransferRequest request, string requestedBy);
        Task<BankStatementResponse> GetBankStatementAsync(int userId, TransactionFilterRequest filter);
        Task<WalletDetailsResponse> GetWalletDetailsAsync(int userId);
        Task<WalletLockResponse> SetWalletLockStateAsync(int walletId, bool isLocked, string requestedBy);
        Task<AdjustBalanceResponse> AdjustBalanceAsync(wallet.Models.Requests.AdjustBalanceRequest request, string requestedBy);
    }
}
