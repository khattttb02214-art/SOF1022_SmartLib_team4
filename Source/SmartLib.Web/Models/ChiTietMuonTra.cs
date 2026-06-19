using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("ChiTietMuonTra")]
public class ChiTietMuonTra
{
    [Key]
    public int Id { get; set; }

    [StringLength(10)]
    public string? MaPhieu { get; set; }

    [StringLength(10)]
    public string? MaSach { get; set; }

    [StringLength(20)]
    public string? MaCuonSach { get; set; }

    public int SoLuong { get; set; } = 1;
    public decimal TienPhat { get; set; } = 0;

    [StringLength(255)]
    public string? GhiChu { get; set; }

    [ForeignKey("MaPhieu")]
    public virtual MuonTra? MuonTra { get; set; }

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }

    [ForeignKey("MaCuonSach")]
    public virtual CuonSach? CuonSach { get; set; }
}
