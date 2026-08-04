using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("ChiTietDatTruoc")]
public class ChiTietDatTruoc
{
    [Key]
    public int MaChiTiet { get; set; }

    public int MaReservation { get; set; }

    [StringLength(10)]
    public string? MaSach { get; set; }

    public int SoLuong { get; set; } = 1;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaReservation")]
    public virtual Reservation? Reservation { get; set; }

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }
}
