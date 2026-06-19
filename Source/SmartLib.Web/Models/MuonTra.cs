using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("MuonTra")]
public class MuonTra
{
    [Key][StringLength(10)]
    public string MaPhieu { get; set; } = null!;

    [StringLength(10)]
    public string? MaDocGia { get; set; }

    [StringLength(10)]
    public string? MaNV { get; set; }

    public DateTime NgayMuon { get; set; } = DateTime.Now;
    public DateTime NgayHenTra { get; set; }
    public DateTime? NgayTraThucTe { get; set; }

    public decimal TienPhat { get; set; } = 0;

    [StringLength(50)]
    public string TrangThai { get; set; } = "Chưa Trả";

    [StringLength(255)]
    public string? GhiChu { get; set; }

    public bool DaGiaHan { get; set; } = false;

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }

    [ForeignKey("MaNV")]
    public virtual NhanVien? NhanVien { get; set; }

    public virtual ICollection<ChiTietMuonTra> ChiTietMuonTras { get; set; } = new List<ChiTietMuonTra>();
}
