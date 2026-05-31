using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class DocGium
{
    public string MaDocGia { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? Lop { get; set; }

    public string? Khoa { get; set; }

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public string? DiaChi { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? MatKhau { get; set; }

    public DateOnly? NgayTaoThe { get; set; }

    public DateOnly? NgayHetHan { get; set; }

    public bool? TrangThaiThe { get; set; }

    public virtual ICollection<DanhGiaSach> DanhGiaSaches { get; set; } = new List<DanhGiaSach>();

    public virtual ICollection<MuonTra> MuonTras { get; set; } = new List<MuonTra>();

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
