using wallet.DALs.Interfaces;
using wallet.Data.Entities;
using wallet.Utils;

namespace wallet.Helpers
{
    public static class TransactionReferenceHelper
    {
        private const string TransferOutSuffix = "-OUT";
        private const string TransferInSuffix = "-IN";

        public static string ResolveReferenceNo(string? referenceNo, string fallbackPrefix)
        {
            return string.IsNullOrWhiteSpace(referenceNo)
                ? IdentifierFactory.ReferenceNo(fallbackPrefix)
                : referenceNo;
        }

        public static async Task<Transaction?> GetExistingTransactionAsync(IUnitOfWork unitOfWork, string referenceNo)
        {
            return await unitOfWork.Transactions.GetAnyByReferenceNoAsync(referenceNo);
        }

        public static (string ReferenceNo, string SenderReferenceNo, string ReceiverReferenceNo) ResolveTransferReferenceNos(string? referenceNo)
        {
            var baseReferenceNo = ResolveReferenceNo(referenceNo, "TRF");

            return (baseReferenceNo, $"{baseReferenceNo}{TransferOutSuffix}", $"{baseReferenceNo}{TransferInSuffix}");
        }
    }
}
