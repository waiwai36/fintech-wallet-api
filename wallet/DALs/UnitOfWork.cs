using Microsoft.EntityFrameworkCore.Storage;
using wallet.DALs.Interfaces;
using wallet.Data;

namespace wallet.DALs
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly WalletdbContext _context;
        private bool _disposed = false;
        public IWalletRepository Wallets { get; }
        public ITransactionRepository Transactions { get; }
        public IUserRepository Users { get; }

        public UnitOfWork(WalletdbContext context)
        {
            _context = context;

            Wallets = new WalletRepository(_context);
            Transactions = new TransactionRepository(_context);
            Users = new UserRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

