using wallet.Constants;
using wallet.DALs.Interfaces;
using wallet.Data.Entities;
using wallet.Exceptions;
using wallet.Helpers;
using wallet.Models.Requests;
using wallet.Models.Responses;
using wallet.Services.Infrastructure;
using wallet.Utils;

namespace wallet.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletValidator _walletValidator;
        private readonly IClock _clock;

        public WalletService(
            IUnitOfWork unitOfWork,
            IWalletValidator validator,
            IClock clock)
        {
            _unitOfWork = unitOfWork;
            _walletValidator = validator;
            _clock = clock;
        }

        #region Manual Bank Deposit;
        public async Task<DepositResponse> InitiateBankDepositAsync(int userId, DepositRequest request, string requestedBy)
        {

            var wallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(userId);
            _walletValidator.ValidateState(wallet);
          
            string referenceNo = IdentifierFactory.ReferenceNo("DEP-BANK");

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                ReferenceNo = referenceNo,
                TransactionType = TransactionConstants.Type.Deposit,
                PaymentMethod = TransactionConstants.PaymentMethod.ManualBankTransfer,
                Amount = request.Amount,
                BeforeBalance = wallet.Balance,
                AfterBalance = wallet.Balance,
                Description = $"[Bank: {request.BankName}] {request.Remark}",
                Status = TransactionConstants.Status.Pending,
                CreatedAt = _clock.UtcNow,
                CreatedBy = requestedBy
            };

            await WalletTransactionHelper.ExecuteAsync(_unitOfWork, async () =>
            { 
                await _unitOfWork.Transactions.AddAsync(transaction);
            }, "Another transaction is in progress. Please wait and try again.");

            return new DepositResponse
            {
                WalletNumber = wallet.WalletNumber,
                Balance = wallet.Balance,
                PaymentMethod = TransactionConstants.PaymentMethod.ManualBankTransfer,
                ReferenceNo = referenceNo,
                Status = TransactionConstants.Status.Pending
            };
        }

        public async Task<DepositResponse> ApproveBankDepositAsync(ApproveDepositRequest request, string requestedBy)
        {
            var transaction = await _unitOfWork.Transactions.GetAnyByReferenceNoAsync(request.ReferenceNo);
            if (transaction == null)
                throw new AppException("Bank deposit record not found.", StatusCodes.Status404NotFound);

            var wallet = await _unitOfWork.Wallets.GetWalletByIdAsync(transaction.WalletId);

            _walletValidator.ValidateState(wallet);
           
            if (transaction.TransactionType != TransactionConstants.Type.Deposit ||
                transaction.PaymentMethod != TransactionConstants.PaymentMethod.ManualBankTransfer)
                throw new AppException("Reference number is not a manual bank deposit.", StatusCodes.Status400BadRequest);

            if (transaction.Status == TransactionConstants.Status.Success)
            {
                return new DepositResponse
                {
                    WalletNumber = wallet.WalletNumber,
                    Balance = wallet.Balance,
                    PaymentMethod = TransactionConstants.PaymentMethod.ManualBankTransfer,
                    ReferenceNo = request.ReferenceNo,
                    Status = TransactionConstants.Status.Success
                };
            }

            if (transaction.Status != TransactionConstants.Status.Pending)
                throw new AppException("Only pending bank deposits can be approved.", StatusCodes.Status400BadRequest);

            decimal beforeBalance = wallet.Balance;
            decimal afterBalance = beforeBalance + transaction.Amount;

            wallet.Balance = afterBalance;
            WalletAuditHelper.TouchForTransaction(wallet, _clock.UtcNow, requestedBy);

            transaction.BeforeBalance = beforeBalance;
            transaction.AfterBalance = afterBalance;
            transaction.Status = TransactionConstants.Status.Success;
            transaction.UpdatedAt = _clock.UtcNow;
            transaction.UpdatedBy = requestedBy;

            await WalletTransactionHelper.ExecuteAsync(_unitOfWork, () =>
            {
                _unitOfWork.Wallets.UpdateWallet(wallet);
                _unitOfWork.Transactions.Update(transaction);

                return Task.CompletedTask;
            }, "Another transaction is in progress. Please wait and try again.");

            return new DepositResponse
            {
                WalletNumber = wallet.WalletNumber,
                Balance = wallet.Balance,
                PaymentMethod = TransactionConstants.PaymentMethod.ManualBankTransfer,
                ReferenceNo = request.ReferenceNo,
                Status = TransactionConstants.Status.Success
            };
        }

        #endregion;
        
        #region Withdraw & Transfer
        public async Task<WithDrawResponse> WithdrawAsync(int userId, WithdrawRequest request, string requestedBy)
        {

            var wallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(userId);
            _walletValidator.ValidateState(wallet);
         
            var referenceNo = TransactionReferenceHelper.ResolveReferenceNo(request.ReferenceNo, "WTH");
            var existingTransaction = await TransactionReferenceHelper.GetExistingTransactionAsync(_unitOfWork, referenceNo);
            
            if (existingTransaction != null)
            {
                if (existingTransaction.Status == TransactionConstants.Status.Success)
                {
                    return new WithDrawResponse
                    {
                        WalletNumber = wallet.WalletNumber,
                        Balance = wallet.Balance,
                        ReferenceNo = existingTransaction.ReferenceNo ?? string.Empty
                    };
                }
            }

            if (wallet.Balance < request.Amount)
                throw new AppException("Insufficient wallet balance.", StatusCodes.Status400BadRequest);

            var (startUtc, endUtc) = _clock.BusinessDayUtcRange;
            var todayWithdrawn = await _unitOfWork.Transactions.GetDailyAmountByTypeAsync(wallet.WalletId, startUtc, endUtc, TransactionConstants.Type.Withdraw);
            if (todayWithdrawn + request.Amount > wallet.DailyWithdrawLimit)
            {
                throw new AppException($"Daily withdrawal limit exceeded (Daily Limit: {wallet.DailyWithdrawLimit}).", StatusCodes.Status400BadRequest);
            }

            decimal beforeBalance = wallet.Balance;
            wallet.Balance -= request.Amount;
            WalletAuditHelper.TouchForTransaction(wallet, _clock.UtcNow, requestedBy);

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                ReferenceNo = referenceNo,
                TransactionType = TransactionConstants.Type.Withdraw,
                Amount = request.Amount,
                BeforeBalance = beforeBalance,
                AfterBalance = wallet.Balance,
                Description = request.Description,
                Status = TransactionConstants.Status.Success,
                CreatedAt = _clock.UtcNow,
                CreatedBy = requestedBy
            };

            await WalletTransactionHelper.ExecuteAsync(_unitOfWork, async () =>
            {
                _unitOfWork.Wallets.UpdateWallet(wallet);
                await _unitOfWork.Transactions.AddAsync(transaction);
            }, "Balance was modified by another transaction. Please try again.");

            return new WithDrawResponse
            {
                WalletNumber = wallet.WalletNumber,
                Balance = wallet.Balance,
                ReferenceNo = transaction.ReferenceNo ?? string.Empty
            };
        }

        public async Task<TransferResponse> TransferAsync(int senderUserId, TransferRequest request, string requestedBy)
        {
            var senderWallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(senderUserId);
            _walletValidator.ValidateState(senderWallet);        
            
             var receiverWallet = await _unitOfWork.Wallets.GetWalletByNumberAsync(request.ReceiverWalletNumber);
            _walletValidator.ValidateState(receiverWallet);
           
            if (senderWallet.WalletNumber == request.ReceiverWalletNumber)
                throw new AppException("Transfers to your own wallet are not allowed.", StatusCodes.Status400BadRequest);
          
            var (referenceNo, senderReferenceNo, receiverReferenceNo) = TransactionReferenceHelper.ResolveTransferReferenceNos(request.ReferenceNo);
            var existingTransaction = await TransactionReferenceHelper.GetExistingTransactionAsync(_unitOfWork, referenceNo);
            
            if (existingTransaction != null)
            {
                if (existingTransaction.Status == TransactionConstants.Status.Success)
                {
                    return new TransferResponse
                    {
                        Status = existingTransaction.Status,
                        ReferenceNo = referenceNo
                    };
                }
            }

            if (senderWallet.Currency != receiverWallet.Currency)
                throw new AppException("Transfers between different currencies are not allowed.", StatusCodes.Status400BadRequest);

            if (senderWallet.Balance < request.Amount)
                throw new AppException("Insufficient wallet balance.", StatusCodes.Status400BadRequest);

            var (startUtc, endUtc) = _clock.BusinessDayUtcRange;
            var todayTransferred = await _unitOfWork.Transactions.GetDailyAmountByTypeAsync(senderWallet.WalletId, startUtc, endUtc, TransactionConstants.Type.TransferOut);
            if (todayTransferred + request.Amount > senderWallet.DailyTransferLimit)
            {
                throw new AppException($"Daily transfer limit exceeded (Daily Limit: {senderWallet.DailyTransferLimit}).", StatusCodes.Status400BadRequest);
            }

            decimal senderBeforeBalance = senderWallet.Balance;
            senderWallet.Balance -= request.Amount;
            WalletAuditHelper.TouchForTransaction(senderWallet, _clock.UtcNow, requestedBy);

            decimal receiverBeforeBalance = receiverWallet.Balance;
            receiverWallet.Balance += request.Amount;
            WalletAuditHelper.TouchForTransaction(receiverWallet, _clock.UtcNow, requestedBy);

            var senderTxn = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = senderWallet.WalletId,
                ReferenceNo = senderReferenceNo,
                TransactionType = TransactionConstants.Type.TransferOut,
                Amount = request.Amount,
                BeforeBalance = senderBeforeBalance,
                AfterBalance = senderWallet.Balance,
                Description = $"To Wallet: {receiverWallet.WalletNumber}",
                Status = TransactionConstants.Status.Success,
                RelatedWalletId = receiverWallet.WalletId,
                CreatedAt = _clock.UtcNow,
                CreatedBy = requestedBy
            };

            var receiverTxn = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = receiverWallet.WalletId,
                ReferenceNo = receiverReferenceNo,
                TransactionType = TransactionConstants.Type.TransferIn,
                Amount = request.Amount,
                BeforeBalance = receiverBeforeBalance,
                AfterBalance = receiverWallet.Balance,
                Description = $"From Wallet: {senderWallet.WalletNumber}",
                Status = TransactionConstants.Status.Success,
                RelatedWalletId = senderWallet.WalletId,
                CreatedAt = _clock.UtcNow,
                CreatedBy = requestedBy
            };

            await WalletTransactionHelper.ExecuteAsync(_unitOfWork, async () =>
            {
                _unitOfWork.Wallets.UpdateWallet(senderWallet);
                _unitOfWork.Wallets.UpdateWallet(receiverWallet);
                await _unitOfWork.Transactions.AddRangeAsync(senderTxn, receiverTxn);
            }, "A conflicting transaction occurred. Please try again.");

            return new TransferResponse
            {
                Status = TransactionConstants.Status.Success,
                ReferenceNo = referenceNo
            };
        }
       
        #endregion;

        #region Reporting & Management
        public async Task<BankStatementResponse> GetBankStatementAsync(int userId, TransactionFilterRequest filter)
        {
            filter.PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            filter.PageSize = filter.PageSize < 1 || filter.PageSize > 100 ? 10 : filter.PageSize;

            var wallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(userId);
            _walletValidator.ValidateState(wallet);
           
            var (opening, totalCredit, totalDebit, closing, totalCount) = await _unitOfWork.Transactions.GetStatementSummaryAsync(wallet.WalletId, filter);
            var transactions = await _unitOfWork.Transactions.GetPagedTransactionsAsync(wallet.WalletId, filter);

            var orderedTransactions = transactions.OrderBy(t => t.CreatedAt).ToList();
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / filter.PageSize);

            return new BankStatementResponse
            {
                Account = new BankStatementAccount
                {
                    AccountNumber = wallet.WalletNumber,
                    AccountHolder = wallet.User?.UserName ?? string.Empty,
                    Currency = wallet.Currency
                },
                Period = new BankStatementPeriod
                {
                    From = filter.FromDate,
                    To = filter.ToDate,
                    GeneratedAt = _clock.UtcNow,
                    CurrentPage = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages,
                    TotalEntries = totalCount
                },
                Summary = new BankStatementSummary
                {
                    OpeningBalance = opening,
                    TotalCredit = totalCredit,
                    TotalDebit = totalDebit,
                    ClosingBalance = closing
                },
                Entries = orderedTransactions.Select(t => new BankStatementEntry
                {
                    CreatedAt = t.CreatedAt,
                    TransactionType = t.TransactionType,
                    Description = t.Description ?? string.Empty,
                    Reference = t.ReferenceNo ?? string.Empty,
                    BeforeBalance = t.BeforeBalance,
                    AfterBalance = t.AfterBalance,
                    Credit = TransactionDirectionHelper.IsCredit(t.TransactionType) ? t.Amount : null,
                    Debit = TransactionDirectionHelper.IsDebit(t.TransactionType) ? t.Amount : null
                }).ToList()
            };
        }

        public async Task<WalletDetailsResponse> GetWalletDetailsAsync(int userId)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(userId);
            _walletValidator.ValidateState(wallet);
           
            return new WalletDetailsResponse
            {
                WalletNumber = wallet.WalletNumber,
                AccountHolder = wallet.User?.UserName ?? string.Empty,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                Status = wallet.Status,
                DailyTransferLimit = wallet.DailyTransferLimit,
                DailyWithdrawLimit = wallet.DailyWithdrawLimit,
                IsLocked = wallet.IsLocked,
                CreatedAt = wallet.CreatedAt
            };
        }

        public async Task<AdjustBalanceResponse> AdjustBalanceAsync(AdjustBalanceRequest request, string requestedBy)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletByNumberAsync(request.WalletNumber);
            _walletValidator.ValidateState(wallet);          

            decimal beforeBalance = wallet.Balance;
            decimal afterBalance = request.Action switch
            {
                AdjustmentAction.Credit => beforeBalance + request.Amount,
                AdjustmentAction.Debit => beforeBalance >= request.Amount
                    ? beforeBalance - request.Amount
                    : throw new AppException("Insufficient wallet balance.", StatusCodes.Status400BadRequest),
                _ => throw new ArgumentException("Invalid adjustment action.")
            };

            wallet.Balance = afterBalance;
            WalletAuditHelper.TouchForTransaction(wallet, _clock.UtcNow, requestedBy);

            string referenceNo = IdentifierFactory.ReferenceNo("ADJ");
            string transactionType = request.Action switch
            {
                AdjustmentAction.Credit => TransactionConstants.Type.Credit,
                AdjustmentAction.Debit => TransactionConstants.Type.Debit,
                _ => throw new ArgumentException("Invalid action for transaction type.")
            };

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                ReferenceNo = referenceNo,
                TransactionType = transactionType,
                PaymentMethod = TransactionConstants.PaymentMethod.AdminAdjustment,
                Amount = request.Amount,
                BeforeBalance = beforeBalance,
                AfterBalance = afterBalance,
                Description = request.Remark,
                Status = TransactionConstants.Status.Success,
                CreatedAt = _clock.UtcNow,
                CreatedBy = requestedBy
            };

            await WalletTransactionHelper.ExecuteAsync(_unitOfWork, async () =>
            {
                _unitOfWork.Wallets.UpdateWallet(wallet);
                await _unitOfWork.Transactions.AddAsync(transaction);
            }, "Balance was modified by another transaction. Please try again.");

            return new AdjustBalanceResponse
            {
                WalletNumber = wallet.WalletNumber,
                Balance = wallet.Balance,
                ReferenceNo = referenceNo,
                Remark = request.Remark
            };
        }

        public async Task<WalletLockResponse> SetWalletLockStateAsync(int walletId, bool isLocked, string requestedBy)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletByIdAsync(walletId);
            if (wallet == null)
                throw new AppException("Wallet account not found.", StatusCodes.Status404NotFound);

            wallet.IsLocked = isLocked;
            wallet.Status = isLocked ? WalletConstants.Status.Blocked : WalletConstants.Status.Active;
            wallet.UpdatedAt = _clock.UtcNow;
            wallet.UpdatedBy = requestedBy;

            _unitOfWork.Wallets.UpdateWallet(wallet);
            await _unitOfWork.SaveChangesAsync();

            return new WalletLockResponse
            {
                WalletId = wallet.WalletId,
                WalletNumber = wallet.WalletNumber,
                IsLocked = wallet.IsLocked,
                Status = wallet.Status
            };
        }
 
       #endregion;
    }
}
