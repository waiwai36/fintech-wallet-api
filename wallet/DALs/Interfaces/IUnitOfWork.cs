using Microsoft.EntityFrameworkCore.Storage;

namespace wallet.DALs.Interfaces
{
    public interface IUnitOfWork
    {
        ITransactionRepository Transactions { get; }
        IWalletRepository Wallets { get; }
        IUserRepository Users { get; }
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();        
    }
}
