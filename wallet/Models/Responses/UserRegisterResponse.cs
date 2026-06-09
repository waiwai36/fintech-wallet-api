namespace wallet.Models.Responses
{
    public class UserRegisterResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;       
        public string WalletNumber { get; set; } = null!;
        public decimal InitialBalance { get; set; } 
        public string Currency { get; set; } = "MMK";
    }
}
