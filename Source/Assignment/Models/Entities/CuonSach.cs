using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Assignment.Models.Entities;

public partial class CuonSach
{
    [Key]
    public string MaCuonSach { get; set; } = null!;

    public string? MaSach { get; set; }

    public string? Barcode { get; set; }

    public string? TrangThai { get; set; }

    public DateOnly? NgayNhap { get; set; }

    public virtual ICollection<ChiTietMuonTra> ChiTietMuonTras { get; set; } = new List<ChiTietMuonTra>();

    public virtual Sach? MaSachNavigation { get; set; }
}
