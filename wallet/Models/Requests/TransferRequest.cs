using System.ComponentModel.DataAnnotations;

namespace wallet.Models.Requests
{
    public class TransferRequest
    {
        [Required]
        public string ReceiverWalletNumber { get; set; }
        
        [Required]
        [Range(1000, 1000000, ErrorMessage = "Transfer amount must be greater than zero.")]         
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? ReferenceNo { get; set; }
    }
}
