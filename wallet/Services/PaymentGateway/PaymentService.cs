using Microsoft.EntityFrameworkCore;
using System.Transactions;
using wallet.Constants;
using wallet.DALs.Interfaces;
using wallet.Data.Entities;
using wallet.Exceptions;
using wallet.Helpers;
using wallet.Models.Requests;
using wallet.Models.Responses;
using static wallet.Constants.TransactionConstants;

namespace wallet.Services.PaymentGateway
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletValidator _walletValidator;

        public PaymentService(IUnitOfWork unitOfWork, IWalletValidator validator)
        {
            _unitOfWork = unitOfWork;
            _walletValidator = validator;
        }

        public async Task<InitPaymentResponse> InitiateDepositAsync(int userId, InitPaymentRequest request, string requestedBy)
        {
          
            var wallet = await _unitOfWork.Wallets.GetWalletByUserIdAsync(userId);
            _walletValidator.ValidateState(wallet);

            Guid uniqueTxnId = Guid.NewGuid();

            var transaction = new Data.Entities.Transaction
            {
                TransactionId = uniqueTxnId, 
                WalletId = wallet.WalletId,
                Amount = request.Amount,       
                TransactionType = TransactionConstants.Type.Deposit,
                PaymentMethod = request.PaymentMethod,
                Description=request.Remark,
                Status = TransactionConstants.Status.Pending,                 
                BeforeBalance = wallet.Balance,               
                AfterBalance = wallet.Balance,                
                CreatedBy = requestedBy,
                CreatedAt = DateTime.UtcNow
            };

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

            var fakePaymentUrl = $"https://fake-gateway.com/pay?orderId={uniqueTxnId}&amount={request.Amount}";

            return new InitPaymentResponse
            {
                TransactionId = uniqueTxnId.ToString(),
                PaymentUrl = fakePaymentUrl,
                Status = TransactionConstants.Status.Pending
            };
        }

        public async Task<GatewayCallbackResponse> ProcessGatewayCallbackAsync(GatewayWebhookRequest request)
        {
            if (!Guid.TryParse(request.MerchantOrderId, out Guid transactionId))
            {
                throw new AppException("Invalid Merchant Order ID format.", StatusCodes.Status400BadRequest);
            }

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var transaction = await _unitOfWork.Transactions.GetByTransactionIdAsync(transactionId);
                if (transaction == null)
                {
                    throw new AppException($"Transaction with ID {transactionId} not found.", StatusCodes.Status404NotFound);
                }

                if (transaction.Status != TransactionConstants.Status.Pending)
                {
                    throw new AppException($"Transaction is already processed. Current status: {transaction.Status}", StatusCodes.Status409Conflict);
                }

                var wallet = await _unitOfWork.Wallets.GetWalletByIdAsync(transaction.WalletId);
                _walletValidator.ValidateState(wallet);

                if (request.StatusCode == "0000") // Success Code
                {
                    transaction.BeforeBalance = wallet!.Balance;
                    wallet.Balance += transaction.Amount;
                    wallet.LastTransactionAt = DateTime.UtcNow;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    wallet.UpdatedBy = wallet.CreatedBy; 

                    transaction.AfterBalance = wallet.Balance;
                    transaction.Status = TransactionConstants.Status.Success;
                    transaction.ReferenceNo = request.GatewayTransactionId;
                    transaction.SettledAt = DateTime.UtcNow;
                }
                else
                {
                    transaction.Status = TransactionConstants.Status.Failed;
                    transaction.SettledAt = DateTime.UtcNow;
                }

                transaction.UpdatedAt = DateTime.UtcNow;
                transaction.UpdatedBy = transaction.CreatedBy;

                _unitOfWork.Wallets.UpdateWallet(wallet!);
                _unitOfWork.Transactions.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return new GatewayCallbackResponse
                {
                    TransactionId = transaction.TransactionId.ToString(),
                    WalletNumber = wallet!.WalletNumber,
                    Balance = wallet.Balance,
                    Status = transaction.Status,
                    ReferenceNo = transaction.ReferenceNo
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException("A conflicting transaction occurred while processing callback. Please try again.", StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}