using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using wallet.DALs.Interfaces;
using wallet.Data;
using wallet.Data.Entities;

namespace wallet.DALs
{
    public class UserRepository : IUserRepository
    {
        private readonly WalletdbContext _context; 

        public UserRepository(WalletdbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }
        public async Task<User?> GetUserByUserNameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        }
        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await _context.Roles.FindAsync(roleId);
        }
        public async Task AddUserAsync(User user)
        {          
            await _context.Users.AddAsync(user);
        }

        public void UpdateUser(User user)
        {         
            _context.Users.Update(user); 
        }

        public void DeleteUser(User user)
        {          
            _context.Users.Remove(user); 
        }
       
    }
}
