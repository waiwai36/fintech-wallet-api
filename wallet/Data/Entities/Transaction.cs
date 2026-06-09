using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace wallet.Data.Entities;

[Index("ReferenceNo", Name = "UQ__Transact__E1A9C144583DB960", IsUnique = true)]
public partial class Transaction
{
    [Key]
    public Guid? TransactionId { get; set; }
    public int WalletId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? ReferenceNo { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string TransactionType { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string PaymentMethod { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal BeforeBalance { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal AfterBalance { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Description { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    public int? RelatedWalletId { get; set; }

    public DateTime? SettledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? UpdatedBy { get; set; }

    [ForeignKey("WalletId")]
    [InverseProperty("Transactions")]
    public virtual Wallet Wallet { get; set; } = null!;
}
