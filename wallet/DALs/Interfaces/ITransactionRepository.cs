using wallet.Data.Entities;
using wallet.Models.Requests;

namespace wallet.DALs.Interfaces
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction);
        Task AddRangeAsync(params Transaction[] transactions);
        Task<Transaction?> GetByTransactionIdAsync(Guid transactionId);
        Task<Transaction?> GetByTransactionByRefNoAsync(string referenceNo);
        void Update(Transaction transaction);
        Task<decimal> GetDailyAmountByTypeAsync(int walletId, DateTime date, string transactionType);
        Task<(decimal OpeningBalance, decimal TotalCredit, decimal TotalDebit, decimal ClosingBalance, int TotalCount)> GetStatementSummaryAsync(int walletId, TransactionFilterRequest filter);
        IQueryable<Transaction> BuildTransactionQuery(int walletId, TransactionFilterRequest filter);
        Task<List<Transaction>> GetPagedTransactionsAsync(int walletId, TransactionFilterRequest filter);
        Task<Transaction?> GetLastTransactionBeforeDateAsync(int walletId, DateTime date);
        Task<Transaction?> GetFirstTransactionAsync(int walletId);
    }
}
