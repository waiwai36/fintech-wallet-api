using Microsoft.EntityFrameworkCore;
using wallet.Constants;
using wallet.DALs.Interfaces;
using wallet.Data;
using wallet.Data.Entities;
using wallet.Models.Requests;

namespace wallet.DALs
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly WalletdbContext _context;

        public TransactionRepository(WalletdbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public async Task AddRangeAsync(params Transaction[] transactions)
        {
            await _context.Transactions.AddRangeAsync(transactions);
        }

        public async Task<Transaction?> GetByTransactionIdAsync(Guid transactionId)
        {
            return await _context.Transactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<Transaction?> GetByTransactionByRefNoAsync(string referenceNo)
        {
            return await _context.Transactions.AsNoTracking()
                   .FirstOrDefaultAsync(t => t.ReferenceNo == referenceNo && t.Status == TransactionConstants.Status.Pending);
        }

        public void Update(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
        }

        public async Task<decimal> GetDailyAmountByTypeAsync(int walletId, DateTime date, string transactionType)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            return await _context.Transactions.AsNoTracking()
                .Where(t => t.WalletId == walletId
                            && t.TransactionType == transactionType
                            && t.Status == TransactionConstants.Status.Success
                            && t.CreatedAt >= startOfDay
                            && t.CreatedAt <= endOfDay)
                .SumAsync(t => t.Amount);
        }

        public async Task<(decimal OpeningBalance, decimal TotalCredit, decimal TotalDebit, decimal ClosingBalance, int TotalCount)> GetStatementSummaryAsync(int walletId, TransactionFilterRequest filter)
        {
            var query = BuildTransactionQuery(walletId, filter);

            var summary = await query
                .GroupBy(t => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    TotalCredit = g.Where(t => t.TransactionType == TransactionConstants.Type.Deposit || t.TransactionType == TransactionConstants.Type.TransferIn || t.TransactionType == "Credit").Sum(t => (decimal?)t.Amount) ?? 0,
                    TotalDebit = g.Where(t => t.TransactionType == TransactionConstants.Type.Withdraw || t.TransactionType == TransactionConstants.Type.TransferOut || t.TransactionType == "Debit").Sum(t => (decimal?)t.Amount) ?? 0
                })
                .FirstOrDefaultAsync();

            int totalCount = summary?.TotalCount ?? 0;
            decimal totalCredit = summary?.TotalCredit ?? 0;
            decimal totalDebit = summary?.TotalDebit ?? 0;

            decimal openingBalance = 0;

            if (filter.FromDate.HasValue)
            {
                var lastBeforePeriod = await GetLastTransactionBeforeDateAsync(walletId, filter.FromDate.Value);
                openingBalance = lastBeforePeriod?.AfterBalance ?? 0;
            }
            else
            {
                var firstTransaction = await GetFirstTransactionAsync(walletId);
                openingBalance = firstTransaction?.BeforeBalance ?? 0;
            }

            var lastInPeriod = await query.OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync();
            decimal closingBalance = lastInPeriod?.AfterBalance ?? openingBalance;

            return (openingBalance, totalCredit, totalDebit, closingBalance, totalCount);
        }

        public IQueryable<Transaction> BuildTransactionQuery(int walletId, TransactionFilterRequest filter)
        {
            var query = _context.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId && t.Status == TransactionConstants.Status.Success);

            if (filter.FromDate.HasValue)
            {
                var fromDate = filter.FromDate.Value.Date;
                query = query.Where(t => t.CreatedAt >= fromDate);
            }

            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.CreatedAt <= toDate);
            }

            return query;
        }

        public async Task<List<Transaction>> GetPagedTransactionsAsync(int walletId, TransactionFilterRequest filter)
        {
            var query = BuildTransactionQuery(walletId, filter);

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }

        public async Task<Transaction?> GetLastTransactionBeforeDateAsync(int walletId, DateTime date)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId && t.Status == TransactionConstants.Status.Success && t.CreatedAt < date)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Transaction?> GetFirstTransactionAsync(int walletId)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId && t.Status == TransactionConstants.Status.Success)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
