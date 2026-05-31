using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class ThongBao
{
    public int MaThongBao { get; set; }

    public string? MaDocGia { get; set; }

    public string? NoiDung { get; set; }

    public bool? DaDoc { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual DocGium? MaDocGiaNavigation { get; set; }
}
