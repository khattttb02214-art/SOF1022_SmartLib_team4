using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("ThongBao")]
public class ThongBao
{
    [Key]
    public int MaThongBao { get; set; }

    [StringLength(10)]
    public string? MaDocGia { get; set; }

    [StringLength(10)]
    public string? MaNV { get; set; }

    [StringLength(200)]
    public string? TieuDe { get; set; }

    [StringLength(500)]
    public string? NoiDung { get; set; }

    /// <summary>'SACH_MOI' | 'HET_HAN' | 'DUOC_DUYET' | 'CHUNG'</summary>
    [StringLength(20)]
    public string? LoaiThongBao { get; set; } = "CHUNG";

    /// <summary>Liên kết đến sách (nếu thông báo về sách)</summary>
    [StringLength(10)]
    public string? MaSach { get; set; }

    public bool DaDoc { get; set; } = false;

    public DateTime NgayTao { get; set; } = DateTime.Now;

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }

    [ForeignKey("MaNV")]
    public virtual NhanVien? NhanVien { get; set; }
}
