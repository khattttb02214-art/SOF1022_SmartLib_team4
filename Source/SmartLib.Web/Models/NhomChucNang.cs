using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

/// <summary>Nhóm chức năng dùng để gom các chức năng liên quan trong màn hình phân quyền (VD: "Danh mục &amp; Kho sách").</summary>
[Table("NhomChucNang")]
public class NhomChucNang
{
    [Key]
    public int MaNhom { get; set; }

    [Required][StringLength(150)]
    public string TenNhom { get; set; } = null!;

    /// <summary>Icon FontAwesome hiển thị trước tên nhóm (VD: fa-book)</summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    public int ThuTu { get; set; }

    public virtual ICollection<ChucNang> ChucNangs { get; set; } = new List<ChucNang>();
}
