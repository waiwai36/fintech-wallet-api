using wallet.Data.Entities;

namespace wallet.Services.Infrastructure
{
    public interface ITokenService
    {
        string CreateAccessToken(User user);
        string GenerateRefreshToken();
    }
}
