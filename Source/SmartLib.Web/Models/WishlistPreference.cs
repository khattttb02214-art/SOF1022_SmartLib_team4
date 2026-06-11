using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("WishlistPreference")]
public class WishlistPreference
{
    [Key]
    public int Id { get; set; }

    [Required][StringLength(10)]
    public string MaDocGia { get; set; } = null!;

    /// <summary>'THELOAI' | 'TACGIA' | 'NXB'</summary>
    [Required][StringLength(20)]
    public string LoaiSoThich { get; set; } = null!;

    /// <summary>MaTheLoai / MaTacGia / MaNXB tương ứng</summary>
    [Required][StringLength(10)]
    public string MaRef { get; set; } = null!;

    public DateTime NgayTao { get; set; } = DateTime.Now;

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }
}
