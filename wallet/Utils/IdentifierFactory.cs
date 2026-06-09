namespace wallet.Utils
{
    public class IdentifierFactory
    {
        public static string ReferenceNo(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";       
        public static string WalletNumber() => $"WL-{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()}";
    }
}
