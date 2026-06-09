using System.Threading.Tasks;
using wallet.Models.Requests;
using wallet.Models.Responses;

namespace wallet.Services
{
    public interface IAuthService
    {
      Task<UserRegisterResponse> RegisterAsync(RegisterRequest request);
      Task<LoginResponse> LoginAsync(LoginRequest request);
      Task LogoutAsync(int userId);
      Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);

    }
}
