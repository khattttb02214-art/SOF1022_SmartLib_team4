using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

/// <summary>
/// Bảng Ebook đã tồn tại sẵn trong CSDL (dùng cho file ebook đính kèm 1 sách) nhưng
/// trước đây chưa có model C# tương ứng, nên Controller không kiểm tra được quan hệ
/// này khi xóa Sách — dẫn tới lỗi "DELETE conflicted with REFERENCE constraint" nếu
/// sách đó có ebook đính kèm. Model này chỉ đủ dùng để đọc/kiểm tra, chưa có màn
/// hình quản lý riêng.
/// </summary>
[Table("Ebook")]
public class Ebook
{
    [Key]
    public int MaEbook { get; set; }

    [StringLength(10)]
    public string? MaSach { get; set; }

    [StringLength(255)]
    public string? TenFile { get; set; }

    [StringLength(255)]
    public string? DuongDanFile { get; set; }

    [StringLength(20)]
    public string? DinhDangFile { get; set; }

    public long? KichThuoc { get; set; }

    public DateTime? NgayTaiLen { get; set; }

    [ForeignKey("MaSach")]
    public virtual Sach? Sach { get; set; }
}
