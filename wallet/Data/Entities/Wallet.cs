using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace wallet.Data.Entities;

[Index("WalletNumber", Name = "UQ__Wallets__677A402C284174E2", IsUnique = true)]
public partial class Wallet
{
    [Key]
    public int WalletId { get; set; }

    public int UserId { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string WalletNumber { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Balance { get; set; }

    [StringLength(5)]
    [Unicode(false)]
    public string Currency { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DailyTransferLimit { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DailyWithdrawLimit { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? LastTransactionAt { get; set; }

    // For Optimistic Concurrency (RowVersion)
    public byte[] RowVersion { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? UpdatedBy { get; set; }

    [InverseProperty("Wallet")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    [ForeignKey("UserId")]
    [InverseProperty("Wallets")]
    public virtual User User { get; set; } = null!;
}
