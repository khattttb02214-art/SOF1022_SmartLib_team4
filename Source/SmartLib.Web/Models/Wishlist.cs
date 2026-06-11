using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("Wishlist")]
public class Wishlist
{
    [Key]
    public int Id { get; set; }

    [StringLength(10)]
    public string? MaDocGia { get; set; }

    [StringLength(10)]
    public string? MaSach { get; set; }

    public DateTime NgayThem { get; set; } = DateTime.Now;

    public int? FolderId { get; set; }

    [ForeignKey("MaDocGia")]
    public virtual DocGia? DocGia { get; set; }

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }

    [ForeignKey("FolderId")]
    public virtual WishlistFolder? Folder { get; set; }
}
