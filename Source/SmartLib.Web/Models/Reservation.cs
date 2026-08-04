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

    public DateTime NgayDat { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string? TrangThai { get; set; } = "Đang Chờ";

    [StringLength(10)]
    public string? MaNV { get; set; }

    [StringLength(255)]
    public string? GhiChu { get; set; }

    /// <summary>Sau khi "Đã Duyệt": true = đã lập phiếu mượn, false = chưa mượn</summary>
    public bool DaMuon { get; set; } = false;

    /// <summary>Mã phiếu mượn được tạo từ đặt trước này (nếu có)</summary>
    [StringLength(20)]
    public string? MaPhieuMuon { get; set; }

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }

    [ForeignKey("MaNV")]
    public virtual NhanVien? NhanVien { get; set; }

    public virtual ICollection<ChiTietDatTruoc> ChiTietDatTruocs { get; set; } = new List<ChiTietDatTruoc>();
}
