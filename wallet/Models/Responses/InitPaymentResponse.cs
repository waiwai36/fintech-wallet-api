namespace wallet.Models.Responses
{
    public class InitPaymentResponse
    {
        public string TransactionId { get; set; } = null!;
        public string PaymentUrl { get; set; } = null!; 
        public string Status { get; set; } = null!;
    }
}
