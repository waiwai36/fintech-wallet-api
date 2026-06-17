using System.Diagnostics.CodeAnalysis;
using wallet.Constants;
using wallet.Data.Entities;
using wallet.Exceptions;

namespace wallet.Helpers
{
    public interface IWalletValidator
    {
        void ValidateState([NotNull] Wallet? wallet);
    }

    public class WalletValidator : IWalletValidator
    {
        public void ValidateState([NotNull] Wallet? wallet)
        {
            if (wallet == null)
                throw new AppException("Wallet account not found.", StatusCodes.Status404NotFound);

            if (wallet.Status != WalletConstants.Status.Active || wallet.IsLocked)
                throw new AppException("Wallet account is invalid or locked", StatusCodes.Status400BadRequest);
        }
    }
}
