using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("TheThuVien")]
public class TheThuVien
{
    [Key]
    public int Id { get; set; }

    [Required][StringLength(20)]
    public string MaThe { get; set; } = null!;

    [StringLength(10)]
    public string? MaDocGia { get; set; }

    public string? AnhThe { get; set; }  // Ảnh thẻ sinh viên

    public DateTime NgayCap { get; set; } = DateTime.Now;

    public DateTime NgayHetHan { get; set; }

    public bool TrangThai { get; set; } = true;

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }
}
