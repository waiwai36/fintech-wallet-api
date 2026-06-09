using System.ComponentModel.DataAnnotations;

namespace wallet.Models.Requests
{
    public class WithdrawRequest
    {
        [Required]
        [Range(1000, 1000000, ErrorMessage = "Withdrawal amount must be greater than zero.")]     
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
