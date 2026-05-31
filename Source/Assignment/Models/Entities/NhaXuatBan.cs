using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Assignment.Models.Entities;

public partial class NhaXuatBan
{
    [Key]
    public string MaNxb { get; set; } = null!;

    public string TenNxb { get; set; } = null!;

    public string? DiaChi { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<Sach> Saches { get; set; } = new List<Sach>();
}
