using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using wallet.Data;
using wallet.Data.Entities;
using wallet.Models.Responses;
using System.Data;
using wallet.DALs.Interfaces;
using System.Security.Cryptography;
using wallet.DALs;
using wallet.Exceptions;
using wallet.Models.Requests;
using wallet.Constants;
using wallet.Utils;
using wallet.Services.Infrastructure;

namespace wallet.Services
{
    public class AuthService : IAuthService
    {  
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        public AuthService(IUnitOfWork unitOfWork,ITokenService tokenService)
        {           
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<UserRegisterResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _unitOfWork.Users.GetUserByEmailAsync(request.Email);

            if (existingUser != null)
            {
                throw new AppException("Email already registered.", StatusCodes.Status400BadRequest);
            }

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RoleId = (int)UserRole.User,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddUserAsync(user);
                await _unitOfWork.SaveChangesAsync(); 

                var currentUtcTime = DateTime.UtcNow;              
                string walletNumber = IdentifierFactory.WalletNumber();

                var wallet = new Wallet
                {
                    UserId = user.UserId,
                    WalletNumber = walletNumber,
                    Balance = 0,
                    Currency = "MMK",
                    Status = WalletConstants.Status.Active,
                    IsLocked = false,
                    CreatedAt = currentUtcTime,
                    CreatedBy = "System_Auto_Register"
                };

                await _unitOfWork.Wallets.AddWalletAsync(wallet);
                await _unitOfWork.SaveChangesAsync();              
                await dbTransaction.CommitAsync();

                return new UserRegisterResponse
                {
                    UserId = wallet.UserId,
                    WalletNumber = wallet.WalletNumber,
                    InitialBalance = wallet.Balance,
                    UserName = user.UserName
                };
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _unitOfWork.Users.GetUserByEmailAsync(request.Email);

            if (user == null)
                throw new AppException("User not found", StatusCodes.Status401Unauthorized);

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new AppException("Invalid password", StatusCodes.Status401Unauthorized);

            return new LoginResponse
            {
                UserName = user.UserName,
                AccessToken = _tokenService.CreateAccessToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };
        }

        public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var user = await _unitOfWork.Users.GetByRefreshTokenAsync(request.RefreshToken);

            if (user == null)
                throw new AppException("Invalid refresh token", StatusCodes.Status401Unauthorized);

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new AppException("Refresh token expired", StatusCodes.Status401Unauthorized);

            return new RefreshTokenResponse
            {
                AccessToken = _tokenService.CreateAccessToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            }; // Rotation Enabled
        }

        public async Task LogoutAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId);
            if (user == null)
                throw new AppException("User not found", StatusCodes.Status404NotFound);

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _unitOfWork.SaveChangesAsync();
        }
        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); 
            await _unitOfWork.SaveChangesAsync();
            return refreshToken;
        }
        
    }
}

