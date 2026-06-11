using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("NhatKyHoatDong")]
public class NhatKyHoatDong
{
    [Key]
    public int MaLog { get; set; }

    [StringLength(10)]
    public string? MaNV { get; set; }

    [StringLength(255)]
    public string? HanhDong { get; set; }

    public string? MoTa { get; set; }

    public DateTime ThoiGian { get; set; } = DateTime.Now;

    [ForeignKey("MaNV")]
    public virtual NhanVien? NhanVien { get; set; }
}
