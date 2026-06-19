using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("TacGia")]
public class TacGia
{
    [Key][StringLength(10)]
    public string MaTacGia { get; set; } = null!;

    [Required][StringLength(100)]
    public string TenTacGia { get; set; } = null!;

    public string? TieuSu { get; set; }

    [StringLength(100)]
    public string? QuocTich { get; set; }

    public virtual ICollection<Sach_TacGia> SachTacGias { get; set; } = new List<Sach_TacGia>();
}
