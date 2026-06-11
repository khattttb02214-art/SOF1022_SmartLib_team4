using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("WishlistFolder")]
public class WishlistFolder
{
    [Key]
    public int Id { get; set; }

    [Required][StringLength(100)]
    public string TenDanhMuc { get; set; } = null!;

    [StringLength(10)]
    public string? MaDocGia { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
