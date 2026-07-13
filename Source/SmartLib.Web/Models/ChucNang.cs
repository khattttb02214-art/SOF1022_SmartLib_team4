using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

/// <summary>Một chức năng (module) cụ thể trong hệ thống mà nhân viên có thể được cấp quyền Xem/Thêm/Sửa/Xóa.</summary>
[Table("ChucNang")]
public class ChucNang
{
    [Key]
    public int MaChucNang { get; set; }

    public int MaNhom { get; set; }

    [Required][StringLength(150)]
    public string TenChucNang { get; set; } = null!;

    /// <summary>Tên Controller tương ứng trong code (dùng để phân quyền tự động ở tầng backend sau này). Có thể để trống.</summary>
    [StringLength(50)]
    public string? Controller { get; set; }

    /// <summary>Icon FontAwesome riêng cho chức năng (nếu để trống sẽ dùng icon của nhóm)</summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    public int ThuTu { get; set; }

    [ForeignKey(nameof(MaNhom))]
    public virtual NhomChucNang? NhomChucNang { get; set; }

    public virtual ICollection<PhanQuyenNhanVien> PhanQuyens { get; set; } = new List<PhanQuyenNhanVien>();
}
