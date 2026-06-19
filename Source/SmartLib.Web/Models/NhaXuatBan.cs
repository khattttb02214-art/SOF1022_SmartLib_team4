using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("NhaXuatBan")]
public class NhaXuatBan
{
    [Key][StringLength(10)]
    public string MaNXB { get; set; } = null!;

    [Required][StringLength(100)]
    public string TenNXB { get; set; } = null!;

    [StringLength(255)]
    public string? DiaChi { get; set; }

    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public virtual ICollection<Sach> Saches { get; set; } = new List<Sach>();
}
