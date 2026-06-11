using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("CuonSach")]
public class CuonSach
{
    [Key]
    [StringLength(20)]
    public string MaCuonSach { get; set; } = null!;

    [StringLength(10)]
    public string? MaSach { get; set; }

    public string? Barcode { get; set; }

    public string? TrangThai { get; set; }

    public DateTime NgayNhap { get; set; }

    [ForeignKey(nameof(MaSach))]
    public virtual Sach? Sach { get; set; }
}