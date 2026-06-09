namespace wallet.Models.Responses
{
    public class AdjustBalanceResponse
    {
        public string WalletNumber { get; set; } = null!;
        public decimal Balance { get; set; }
        public string ReferenceNo { get; set; } = null!;
        public string? Remark { get; set; }
    }
}
