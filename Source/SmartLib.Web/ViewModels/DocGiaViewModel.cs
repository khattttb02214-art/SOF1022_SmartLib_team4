using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class DocGiaViewModel
{
    public string? MaDocGia { get; set; }

    [Required(ErrorMessage = "Họ tên không được trống")]
    public string HoTen { get; set; } = null!;

    public string? Lop { get; set; }
    public string? Khoa { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public DateTime? NgaySinh { get; set; }
    public DateTime? NgayHetHan { get; set; }

    // Mã thẻ thư viện
    [Required(ErrorMessage = "Mã thẻ thư viện không được trống")]
    public string? MaTheTV { get; set; }

    public bool TaoTaiKhoan { get; set; }

    [EmailAddress]
    public string? EmailTaiKhoan { get; set; }
    public string? MatKhau { get; set; }

    public IFormFile? AnhDaiDienFile { get; set; }
}
