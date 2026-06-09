using Microsoft.EntityFrameworkCore.Storage;
using wallet.Data.Entities;

namespace wallet.DALs.Interfaces
{
    public interface IUserRepository
    {              
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUserNameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
        Task AddUserAsync(User user);
        void UpdateUser(User user);
        void DeleteUser(User user);   
        Task<Role?> GetRoleByIdAsync(int roleId);      

    }
}
