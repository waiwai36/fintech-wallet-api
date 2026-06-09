using System.ComponentModel.DataAnnotations;

namespace wallet.Models.Requests
{
    public class InitPaymentRequest
    {
        [Required]
        [Range(1000, 1000000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = null!;

        public string Remark { get; set; }
    }
}
