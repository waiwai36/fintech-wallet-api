namespace wallet.Models.Responses
{
    public class WalletDetailsResponse
    {
        public string WalletNumber { get; set; }
        public string AccountHolder { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public decimal DailyTransferLimit { get; set; }
        public decimal DailyWithdrawLimit { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
