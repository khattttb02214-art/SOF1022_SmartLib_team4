using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("TheLoai")]
public class TheLoai
{
    [Key]
    [StringLength(10)]
    public string MaTheLoai { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string TenTheLoai { get; set; } = null!;

    [StringLength(500)]
    public string? MoTa { get; set; }

    public ICollection<Sach> Saches { get; set; }
        = new List<Sach>();
}