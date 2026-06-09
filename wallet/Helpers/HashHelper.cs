using System.Security.Cryptography;
using System.Text;

namespace wallet.Helpers
{
    public class HashHelper
    { 
         /// <summary>
        /// Generate an MD5 signature by concatenating MerchantOrderId, Amount, StatusCode, and SecretKey.
       /// </summary>
        public static string GenerateMd5Signature(string merchantOrderId, decimal amount, string statusCode, string secretKey)
        {
            string rawData = $"{merchantOrderId}{amount}{statusCode}{secretKey}";

            // 2. Compute the MD5 hash of the combined data.
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawData);
            byte[] hashBytes = MD5.HashData(inputBytes);

            // 3. Convert the byte array to a hexadecimal string (uppercase output).
            StringBuilder sb = new();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Verify whether the incoming gateway signature matches the locally computed signature.
        /// </summary>
        public static bool VerifySignature(string merchantOrderId, decimal amount, string statusCode, string secretKey, string incomingHash)
        {
            if (string.IsNullOrEmpty(incomingHash)) return false;

            string computedHash = GenerateMd5Signature(merchantOrderId, amount, statusCode, secretKey);

            // Compare hashes case-insensitively to avoid mismatches from uppercase/lowercase hex formatting.
            return string.Equals(computedHash, incomingHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
