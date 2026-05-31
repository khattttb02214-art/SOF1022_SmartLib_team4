using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class NhatKyHoatDong
{
    public int MaLog { get; set; }

    public string? MaNv { get; set; }

    public string? HanhDong { get; set; }

    public string? MoTa { get; set; }

    public DateTime? ThoiGian { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }
}
