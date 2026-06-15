using wallet.Data.Entities;

namespace wallet.Helpers
{
    public static class WalletAuditHelper
    {
        public static void TouchForTransaction(Wallet wallet, DateTime utcNow, string requestedBy)
        {
            wallet.LastTransactionAt = utcNow;
            wallet.UpdatedAt = utcNow;
            wallet.UpdatedBy = requestedBy;
        }
    }
}
