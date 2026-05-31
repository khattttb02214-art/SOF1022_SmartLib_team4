using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class TacGium
{
    public string MaTacGia { get; set; } = null!;

    public string TenTacGia { get; set; } = null!;

    public string? TieuSu { get; set; }

    public string? QuocTich { get; set; }

    public virtual ICollection<Sach> MaSaches { get; set; } = new List<Sach>();
}
