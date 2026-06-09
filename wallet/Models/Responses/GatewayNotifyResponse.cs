namespace wallet.Models.Responses
{
    public class GatewayNotifyResponse
    {
        public bool IsSuccess { get; set; }
        public string TargetUrl { get; set; } = null!;
    }
}
