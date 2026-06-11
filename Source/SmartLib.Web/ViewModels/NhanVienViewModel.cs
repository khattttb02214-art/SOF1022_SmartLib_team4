using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class NhanVienViewModel
{
    public string? MaNV { get; set; }

    [Required(ErrorMessage = "Họ tên không được trống")]
    public string HoTen { get; set; } = null!;

    [Required(ErrorMessage = "Email không được trống")]
    [EmailAddress]
    public string Email { get; set; } = null!;

    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }

    [StringLength(10)]
    public string? MaChucVu { get; set; }

    public bool TrangThai { get; set; } = true;

    // Chỉ dùng khi tạo mới
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
    public string? MatKhau { get; set; }

    public IFormFile? AnhDaiDienFile { get; set; }
}
