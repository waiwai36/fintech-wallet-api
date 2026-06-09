namespace wallet.Models.Responses
{
    public class GatewayCallbackResponse
    {     
        public string TransactionId { get; set; } 
        public string WalletNumber { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
        public string ReferenceNo { get; set; }
    }
}
