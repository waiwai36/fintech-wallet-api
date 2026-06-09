using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace wallet.Data.Entities;

public partial class RoleClaim
{
    [Key]
    public int Id { get; set; }

    public int RoleId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string ClaimType { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string ClaimValue { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("RoleClaims")]
    public virtual Role Role { get; set; } = null!;
}
