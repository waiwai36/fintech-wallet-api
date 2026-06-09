using Microsoft.EntityFrameworkCore;
using wallet.Constants;
using wallet.DALs.Interfaces;
using wallet.Data.Entities;
using wallet.Exceptions;
using wallet.Helpers;
using wallet.Models.Requests;
using wallet.Models.Responses;
using wallet.Utils;

namespace wallet.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletValidator _walletValidator;

        public WalletService(IUnitOfWork unitOfWork, IWalletValidator validator)
        {
            _unitOfWork = unitOfWork;
            _walletValidator = validator;
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
                CreatedAt = DateTime.UtcNow,
                CreatedBy = requestedBy
            };

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException("Another transaction is in progress. Please wait and try again.", StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

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
            var transaction = await _unitOfWork.Transactions.GetByTransactionByRefNoAsync(request.ReferenceNo);
            if (transaction == null)
                throw new AppException("Pending bank deposit record not found.", StatusCodes.Status404NotFound);

            var wallet = await _unitOfWork.Wallets.GetWalletByIdAsync(transaction.WalletId);

            _walletValidator.ValidateState(wallet);

            decimal beforeBalance = wallet.Balance;
            decimal afterBalance = beforeBalance + transaction.Amount;

            wallet.Balance = afterBalance;
            wallet.LastTransactionAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;
            wallet.UpdatedBy = requestedBy;

            transaction.BeforeBalance = beforeBalance;
            transaction.AfterBalance = afterBalance;
            transaction.Status = TransactionConstants.Status.Success;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.UpdatedBy = requestedBy;

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Wallets.UpdateWallet(wallet);
                _unitOfWork.Transactions.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException("Another transaction is in progress. Please wait and try again.", StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

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

        public async Task<WithDrawResponse> WithdrawAsync(int userId, WithdrawRequest requset, string requestedBy)
        {

            var wallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(userId);
            _walletValidator.ValidateState(wallet);

            if (wallet!.Balance < requset.Amount)
                throw new AppException("Insufficient wallet balance.", StatusCodes.Status400BadRequest);

            var todayWithdrawn = await _unitOfWork.Transactions.GetDailyAmountByTypeAsync(wallet.WalletId, DateTime.UtcNow, TransactionConstants.Type.Withdraw);
            if (todayWithdrawn + requset.Amount > wallet.DailyWithdrawLimit)
            {
                throw new AppException($"Daily withdrawal limit exceeded (Daily Limit: {wallet.DailyWithdrawLimit}).", StatusCodes.Status400BadRequest);
            }

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                decimal beforeBalance = wallet.Balance;
                wallet.Balance -= requset.Amount;
                wallet.LastTransactionAt = DateTime.UtcNow;
                wallet.UpdatedAt = DateTime.UtcNow;
                wallet.UpdatedBy = requestedBy;

                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    ReferenceNo = IdentifierFactory.ReferenceNo("WTH"),
                    TransactionType = TransactionConstants.Type.Withdraw,
                    Amount = requset.Amount,
                    BeforeBalance = beforeBalance,
                    AfterBalance = wallet.Balance,
                    Description = requset.Description,
                    Status = TransactionConstants.Status.Success,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = requestedBy
                };

                _unitOfWork.Wallets.UpdateWallet(wallet);
                await _unitOfWork.Transactions.AddAsync(transaction);

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new WithDrawResponse
                {
                    WalletNumber = wallet.WalletNumber,
                    Balance = wallet.Balance,
                    ReferenceNo = transaction.ReferenceNo
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException("Balance was modified by another transaction. Please try again.", StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TransferResponse> TransferAsync(int senderUserId, TransferRequest request, string requestedBy)
        {
            var senderWallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(senderUserId);
            _walletValidator.ValidateState(senderWallet);

            if (senderWallet!.WalletNumber == request.ReceiverWalletNumber)
                throw new AppException("Transfers to your own wallet are not allowed.", StatusCodes.Status400BadRequest);

            var receiverWallet = await _unitOfWork.Wallets.GetWalletByNumberAsync(request.ReceiverWalletNumber);
            _walletValidator.ValidateState(receiverWallet);

            if (senderWallet.Currency != receiverWallet!.Currency)
                throw new AppException("Transfers between different currencies are not allowed.", StatusCodes.Status400BadRequest);

            if (senderWallet.Balance < request.Amount)
                throw new AppException("Insufficient wallet balance.", StatusCodes.Status400BadRequest);

            var todayTransferred = await _unitOfWork.Transactions.GetDailyAmountByTypeAsync(senderWallet.WalletId, DateTime.UtcNow, TransactionConstants.Type.TransferOut);
            if (todayTransferred + request.Amount > senderWallet.DailyTransferLimit)
            {
                throw new AppException($"Daily transfer limit exceeded (Daily Limit: {senderWallet.DailyTransferLimit}).", StatusCodes.Status400BadRequest);
            }

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                decimal senderBeforeBalance = senderWallet.Balance;
                senderWallet.Balance -= request.Amount;
                senderWallet.LastTransactionAt = DateTime.UtcNow;
                senderWallet.UpdatedAt = DateTime.UtcNow;
                senderWallet.UpdatedBy = requestedBy;

                decimal receiverBeforeBalance = receiverWallet.Balance;
                receiverWallet.Balance += request.Amount;
                receiverWallet.LastTransactionAt = DateTime.UtcNow;
                receiverWallet.UpdatedAt = DateTime.UtcNow;
                receiverWallet.UpdatedBy = requestedBy;

                string referenceNo = IdentifierFactory.ReferenceNo("TRF");

                var senderTxn = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = senderWallet.WalletId,
                    ReferenceNo = referenceNo,
                    TransactionType = TransactionConstants.Type.TransferOut,
                    Amount = request.Amount,
                    BeforeBalance = senderBeforeBalance,
                    AfterBalance = senderWallet.Balance,
                    Description = $"To Wallet: {receiverWallet.WalletNumber}",
                    Status = TransactionConstants.Status.Success,
                    RelatedWalletId = receiverWallet.WalletId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = requestedBy
                };

                var receiverTxn = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = receiverWallet.WalletId,
                    ReferenceNo = referenceNo,
                    TransactionType = TransactionConstants.Type.TransferIn,
                    Amount = request.Amount,
                    BeforeBalance = receiverBeforeBalance,
                    AfterBalance = receiverWallet.Balance,
                    Description = $"From Wallet: {senderWallet.WalletNumber}",
                    Status = TransactionConstants.Status.Success,
                    RelatedWalletId = senderWallet.WalletId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = requestedBy
                };

                _unitOfWork.Wallets.UpdateWallet(senderWallet);
                _unitOfWork.Wallets.UpdateWallet(receiverWallet);
                await _unitOfWork.Transactions.AddRangeAsync(senderTxn, receiverTxn);

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new TransferResponse
                {
                    Status = TransactionConstants.Status.Success,
                    ReferenceNo = referenceNo
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException("A conflicting transaction occurred. Please try again.", StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

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
                    AccountHolder = wallet.User?.UserName,
                    Currency = wallet.Currency
                },
                Period = new BankStatementPeriod
                {
                    From = filter.FromDate,
                    To = filter.ToDate,
                    GeneratedAt = DateTime.UtcNow,
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
                    Credit = (t.TransactionType == TransactionConstants.Type.Deposit || t.TransactionType == TransactionConstants.Type.TransferIn || t.TransactionType == "Credit") ? t.Amount : null,
                    Debit = (t.TransactionType == TransactionConstants.Type.Withdraw || t.TransactionType == TransactionConstants.Type.TransferOut || t.TransactionType == "Debit") ? t.Amount : null
                }).ToList()
            };
        }

        public async Task<WalletDetailsResponse> GetWalletDetailsAsync(int userId)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(userId);

            return new WalletDetailsResponse
            {
                WalletNumber = wallet.WalletNumber,
                AccountHolder = wallet.User.UserName,
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

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                wallet.Balance = afterBalance;
                wallet.LastTransactionAt = DateTime.UtcNow;
                wallet.UpdatedAt = DateTime.UtcNow;
                wallet.UpdatedBy = requestedBy;

                string referenceNo = IdentifierFactory.ReferenceNo("ADJ");
                string transactionType = request.Action switch
                {
                    AdjustmentAction.Credit => "Credit",
                    AdjustmentAction.Debit => "Debit",
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
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = requestedBy
                };

                _unitOfWork.Wallets.UpdateWallet(wallet);
                await _unitOfWork.Transactions.AddAsync(transaction);

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new AdjustBalanceResponse
                {
                    WalletNumber = wallet.WalletNumber,
                    Balance = wallet.Balance,
                    ReferenceNo = referenceNo,
                    Remark = request.Remark
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException("Balance was modified by another transaction. Please try again.", StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<WalletLockResponse> SetWalletLockStateAsync(int walletId, bool isLocked, string requestedBy)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletByIdAsync(walletId);
            _walletValidator.ValidateState(wallet);

            wallet.IsLocked = isLocked;
            wallet.Status = isLocked ? WalletConstants.Status.Blocked : WalletConstants.Status.Active;
            wallet.UpdatedAt = DateTime.UtcNow;
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
    }
}
