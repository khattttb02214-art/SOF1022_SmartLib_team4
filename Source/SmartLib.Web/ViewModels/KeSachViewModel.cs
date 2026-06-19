using SmartLib.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class KeSachViewModel
{
    // Không [Required] vì MaKe có thể để trống để tự động tạo
    [StringLength(10)]
    public string? MaKe { get; set; }

    [Required(ErrorMessage = "Tên kệ không được trống")]
    [StringLength(100)]
    public string TenKe { get; set; } = null!;

    [StringLength(200)]
    public string? ViTri { get; set; }

    [Range(1, 20, ErrorMessage = "Tầng từ 1 đến 20")]
    public int? Tang { get; set; }

    [StringLength(50)]
    public string? Phong { get; set; }

    [StringLength(200)]
    public string? MoTa { get; set; }

    [Range(1, 10000)]
    public int? SucChua { get; set; }

    public string? MaNXBPhuTrach { get; set; }
    public string? MaTheLoaiPhuTrach { get; set; }

    public bool TrangThai { get; set; } = true;

    // Thống kê (chỉ dùng ở Index)
    public int SoSach { get; set; }
    public int SoCuon { get; set; }
    public string? TenNXBPhuTrach { get; set; }
    public string? TenTheLoaiPhuTrach { get; set; }
}
