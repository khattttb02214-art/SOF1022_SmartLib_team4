using System;
using System.Collections.Generic;

namespace Assignment.Models.Entities;

public partial class Wishlist
{
    public string MaDocGia { get; set; } = null!;

    public string MaSach { get; set; } = null!;

    public DateOnly? NgayThem { get; set; }

    public virtual DocGium MaDocGiaNavigation { get; set; } = null!;

    public virtual Sach MaSachNavigation { get; set; } = null!;
}
