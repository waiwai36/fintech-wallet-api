namespace wallet.Models.Requests
{
    public class GatewayNotifyRequest
    {
        public string MerchantOrderId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string StatusCode { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string HashSign { get; set; } = null!;

    }
}
