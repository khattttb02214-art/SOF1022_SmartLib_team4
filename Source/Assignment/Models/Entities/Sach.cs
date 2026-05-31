using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class Sach
{
    public string MaSach { get; set; } = null!;

    public string? Isbn { get; set; }

    public string? Barcode { get; set; }

    public string TenSach { get; set; } = null!;

    public string? MaTheLoai { get; set; }

    public string? MaNxb { get; set; }

    public int? NamXuatBan { get; set; }

    public string? NgonNgu { get; set; }

    public int? SoTrang { get; set; }

    public string? MoTa { get; set; }

    public string? AnhBia { get; set; }

    public int? SoLuongKho { get; set; }

    public int? SoLuongKhaDung { get; set; }

    public string? MaKe { get; set; }

    public bool? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<CuonSach> CuonSaches { get; set; } = new List<CuonSach>();

    public virtual ICollection<DanhGiaSach> DanhGiaSaches { get; set; } = new List<DanhGiaSach>();

    public virtual ICollection<Ebook> Ebooks { get; set; } = new List<Ebook>();

    public virtual KeSach? MaKeNavigation { get; set; }

    public virtual NhaXuatBan? MaNxbNavigation { get; set; }

    public virtual TheLoai? MaTheLoaiNavigation { get; set; }

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public virtual ICollection<TacGium> MaTacGia { get; set; } = new List<TacGium>();
}
