using System.ComponentModel.DataAnnotations;

namespace wallet.Models.Requests
{
    public enum AdjustmentAction
    {
        Credit, 
        Debit   
    }
    public class AdjustBalanceRequest
    {
        [Required]
        public string WalletNumber { get; set; } = null!;

        [Required]
        [Range(1000, 1000000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        // true = credit (add), false = debit (subtract)
        [Required]
        public AdjustmentAction Action { get; set; }

        public string? Remark { get; set; }
    }
}
