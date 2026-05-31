using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class TheLoai
{
    public string MaTheLoai { get; set; } = null!;

    public string TenTheLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<Sach> Saches { get; set; } = new List<Sach>();
}
