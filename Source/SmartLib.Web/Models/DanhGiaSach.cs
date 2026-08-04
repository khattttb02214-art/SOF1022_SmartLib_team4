using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("DanhGiaSach")]
public class DanhGiaSach
{
    [Key]
    public int MaDanhGia { get; set; }

    [StringLength(10)]
    public string? MaDocGia { get; set; }

    [StringLength(10)]
    public string? MaSach { get; set; }

    [Range(1, 5)]
    public int SoSao { get; set; }

    [StringLength(1000)]
    public string? NoiDung { get; set; }

    public DateTime NgayDanhGia { get; set; } = DateTime.Now;

    /// <summary>
    /// Status: "Hiển thị" or "Đã xóa"
    /// </summary>
    [StringLength(50)]
    public string? TrangThai { get; set; } = "Hiển thị";

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }
}
