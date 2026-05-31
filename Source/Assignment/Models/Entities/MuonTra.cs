using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class MuonTra
{
    public string MaPhieu { get; set; } = null!;

    public string? MaDocGia { get; set; }

    public string? MaNv { get; set; }

    public DateOnly? NgayMuon { get; set; }

    public DateOnly NgayHenTra { get; set; }

    public DateOnly? NgayTraThucTe { get; set; }

    public decimal? TienPhat { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<ChiTietMuonTra> ChiTietMuonTras { get; set; } = new List<ChiTietMuonTra>();

    public virtual DocGium? MaDocGiaNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }
}
