namespace wallet.Models.Responses
{
    public class WithDrawResponse
    {
        public string WalletNumber { get; set; }

        public decimal Balance { get; set; }

        public string ReferenceNo { get; set; }
    }
}
