using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class ChiTietMuonTra
{
    public string MaPhieu { get; set; } = null!;

    public string MaCuonSach { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual CuonSach MaCuonSachNavigation { get; set; } = null!;

    public virtual MuonTra MaPhieuNavigation { get; set; } = null!;
}
