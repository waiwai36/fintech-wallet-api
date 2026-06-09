using Microsoft.EntityFrameworkCore;
using wallet.Constants;
using wallet.DALs.Interfaces;
using wallet.Data;
using wallet.Data.Entities;
using wallet.Exceptions;
using wallet.Models.Requests;

namespace wallet.DALs
{
    public class WalletRepository : IWalletRepository
    {
        private readonly WalletdbContext _context;      
        public WalletRepository(WalletdbContext context)
        {
            _context = context;           
        }

        public async Task AddWalletAsync(Wallet wallet)
        {
            await _context.Wallets.AddAsync(wallet);
        }

        public void UpdateWallet(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
        }

        public Task<Wallet?> GetWalletByUserIdAsync(int userId)
        {
            return _context.Wallets.Include(u => u.User).FirstOrDefaultAsync(w => w.UserId == userId);
        }
       
        public Task<Wallet?> GetWalletByIdAsync(int walletId)
        {
            return _context.Wallets.AsNoTracking().SingleOrDefaultAsync(w => w.WalletId == walletId);
        }
        
        public Task<Wallet?> GetWalletByNumberAsync(string walletNumber)
        {
            return _context.Wallets.FirstOrDefaultAsync(w => w.WalletNumber == walletNumber);
        }
      
        public Task<Wallet?> GetWalletDetailsByUserIdAsync(int userId)
        {
            return _context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId);
        }
      }   
}
