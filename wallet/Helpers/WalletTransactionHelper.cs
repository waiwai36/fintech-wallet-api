using Microsoft.EntityFrameworkCore;
using wallet.DALs.Interfaces;
using wallet.Exceptions;

namespace wallet.Helpers
{
    public static class WalletTransactionHelper
    {
        public static async Task ExecuteAsync(IUnitOfWork unitOfWork, Func<Task> action, string concurrencyMessage)
        {
            using var dbTransaction = await unitOfWork.BeginTransactionAsync();
            try
            {
                await action();
                await unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException(concurrencyMessage, StatusCodes.Status409Conflict);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ__Transact", StringComparison.OrdinalIgnoreCase) == true)
            {
                await dbTransaction.RollbackAsync();
                throw new AppException("Duplicate transaction reference. Please retry with a different idempotency key.", StatusCodes.Status409Conflict);
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
