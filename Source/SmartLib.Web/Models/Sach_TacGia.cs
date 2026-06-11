using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("Sach_TacGia")]
public class Sach_TacGia
{
    [StringLength(10)]
    public string MaSach { get; set; } = null!;

    [StringLength(10)]
    public string MaTacGia { get; set; } = null!;

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }

    [ForeignKey("MaTacGia")]
    public virtual TacGia? TacGia { get; set; }
}
