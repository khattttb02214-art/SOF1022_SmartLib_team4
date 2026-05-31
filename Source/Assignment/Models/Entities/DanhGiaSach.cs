using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class DanhGiaSach
{
    public int MaDanhGia { get; set; }

    public string? MaDocGia { get; set; }

    public string? MaSach { get; set; }

    public int? SoSao { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public virtual DocGium? MaDocGiaNavigation { get; set; }

    public virtual Sach? MaSachNavigation { get; set; }
}
