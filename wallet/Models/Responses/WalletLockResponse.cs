namespace wallet.Models.Responses
{
    public class WalletLockResponse
    {
        public int WalletId { get; set; }
        public string WalletNumber { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
