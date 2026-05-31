using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Assignment.Models.Entities;

public partial class NhanVien
{
    [Key]
    public string MaNv { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }

    public string? DiaChi { get; set; }

    // Sửa dòng này trong file NhanVien.cs
    public string? MatKhau { get; set; }

    public string? AnhDaiDien { get; set; }

    public string? MaChucVu { get; set; }

    public bool? TrangThai { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<BackupDuLieu> BackupDuLieus { get; set; } = new List<BackupDuLieu>();

    public virtual ChucVu? MaChucVuNavigation { get; set; }

    public virtual ICollection<MuonTra> MuonTras { get; set; } = new List<MuonTra>();

    public virtual ICollection<NhatKyHoatDong> NhatKyHoatDongs { get; set; } = new List<NhatKyHoatDong>();
}
