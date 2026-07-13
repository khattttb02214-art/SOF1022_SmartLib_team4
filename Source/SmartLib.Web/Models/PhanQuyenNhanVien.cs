using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

/// <summary>
/// Ma trận phân quyền chi tiết: mỗi dòng = quyền của 1 nhân viên trên 1 chức năng cụ thể.
/// Khác với mô hình cấp bậc (Không truy cập/Xem/Sửa/Thêm/Toàn quyền) chỉ chọn được 1 mức,
/// ở đây 4 quyền Xem/Thêm/Sửa/Xóa là ĐỘC LẬP — cho phép ví dụ nhân viên A được Thêm+Sửa nhưng
/// không được Xóa, còn nhân viên B được Sửa+Xóa nhưng không được Thêm, dù cùng chức vụ.
/// </summary>
[Table("PhanQuyenNhanVien")]
public class PhanQuyenNhanVien
{
    [Key]
    public int MaPQ { get; set; }

    [Required][StringLength(10)]
    public string MaNV { get; set; } = null!;

    public int MaChucNang { get; set; }

    public bool DuocXem { get; set; }
    public bool DuocThem { get; set; }
    public bool DuocSua { get; set; }
    public bool DuocXoa { get; set; }

    [ForeignKey(nameof(MaNV))]
    public virtual NhanVien? NhanVien { get; set; }

    [ForeignKey(nameof(MaChucNang))]
    public virtual ChucNang? ChucNang { get; set; }
}
