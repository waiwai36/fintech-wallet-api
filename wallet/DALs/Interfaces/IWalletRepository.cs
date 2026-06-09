using wallet.Data.Entities;
using wallet.Models.Requests;

namespace wallet.DALs.Interfaces
{
    public interface IWalletRepository
    {
        Task AddWalletAsync(Wallet wallet);
        void UpdateWallet(Wallet wallet);
        Task<Wallet?> GetWalletByUserIdAsync(int userId);
        Task<Wallet?> GetWalletByIdAsync(int walletId);
        Task<Wallet?> GetWalletByNumberAsync(string walletNumber);
        Task<Wallet?> GetWalletDetailsByUserIdAsync(int userId);
        
    }
}
