using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class Ebook
{
    public int MaEbook { get; set; }

    public string? MaSach { get; set; }

    public string? TenFile { get; set; }

    public string? DuongDanFile { get; set; }

    public string? DinhDangFile { get; set; }

    public long? KichThuoc { get; set; }

    public DateTime? NgayTaiLen { get; set; }

    public virtual Sach? MaSachNavigation { get; set; }
}
