using wallet.Constants;

namespace wallet.Helpers
{
    public static class TransactionDirectionHelper
    {
        public static bool IsCredit(string transactionType)
        {
            return transactionType == TransactionConstants.Type.Deposit ||
                   transactionType == TransactionConstants.Type.TransferIn ||
                   transactionType == TransactionConstants.Type.Credit;
        }

        public static bool IsDebit(string transactionType)
        {
            return transactionType == TransactionConstants.Type.Withdraw ||
                   transactionType == TransactionConstants.Type.TransferOut ||
                   transactionType == TransactionConstants.Type.Debit;
        }
    }
}
