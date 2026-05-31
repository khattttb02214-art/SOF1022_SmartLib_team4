using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Assignment.Models.Entities;

public partial class KeSach
{
    [Key]
    public string MaKe { get; set; } = null!;

    public string TenKe { get; set; } = null!;

    public string? ViTri { get; set; }

    public virtual ICollection<Sach> Saches { get; set; } = new List<Sach>();
}
