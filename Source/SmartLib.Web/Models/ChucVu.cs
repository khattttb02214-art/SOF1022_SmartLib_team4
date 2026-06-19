using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("ChucVu")]
public class ChucVu
{
    [Key]
    [StringLength(10)]
    public string MaChucVu { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string TenChucVu { get; set; } = null!;
}