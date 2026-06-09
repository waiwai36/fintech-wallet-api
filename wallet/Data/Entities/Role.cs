using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace wallet.Data.Entities;

public partial class Role
{
    [Key]
    public int RoleId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string RoleName { get; set; } = null!;

    [InverseProperty("Role")]
    public virtual ICollection<RoleClaim> RoleClaims { get; set; } = new List<RoleClaim>();

    [InverseProperty("Role")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
