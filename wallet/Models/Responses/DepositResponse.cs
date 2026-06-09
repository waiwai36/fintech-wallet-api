using System.Runtime.CompilerServices;

namespace wallet.Models.Responses
{
    public class DepositResponse
    {
        public string WalletNumber { get; set; }

        public decimal Balance { get; set; }

        public String PaymentMethod { get; set; }

        public string ReferenceNo { get; set; }

        public string Status { get; set; }

    }
}
