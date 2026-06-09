using System.ComponentModel.DataAnnotations;

namespace wallet.Models.Requests
{
    public class ApproveDepositRequest
    {
        [Required]
        public string ReferenceNo { get; set; } = null!;
    }
}
