using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("Reservation")]
public class Reservation
{
    [Key]
    public int MaReservation { get; set; }

    [StringLength(10)]
    public string? MaDocGia { get; set; }

    [StringLength(10)]
    public string? MaSach { get; set; }

    public DateTime NgayDat { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string? TrangThai { get; set; }

    /// <summary>Sau khi "Đã Duyệt": true = đã lập phiếu mượn, false = chưa mượn</summary>
    public bool DaMuon { get; set; } = false;

    /// <summary>Mã phiếu mượn được tạo từ đặt trước này (nếu có)</summary>
    [StringLength(20)]
    public string? MaPhieuMuon { get; set; }

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }
}
