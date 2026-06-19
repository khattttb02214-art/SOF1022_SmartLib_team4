using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class BookViewModel
{
    // Không [Required] – sẽ được auto-gen trong controller
    [StringLength(10)]
    public string? MaSach { get; set; }

    [Required(ErrorMessage = "Tên sách không được trống")]
    [StringLength(200)]
    public string TenSach { get; set; } = null!;

    [StringLength(20)]
    public string? ISBN { get; set; }

    [StringLength(100)]
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "Thể loại không được trống")]
    public string? MaTheLoai { get; set; }

    [Required(ErrorMessage = "Nhà xuất bản không được trống")]
    public string? MaNXB { get; set; }

    [Required(ErrorMessage = "Kệ sách không được trống")]
    public string? MaKe { get; set; }

    [Range(1800, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    public int? NamXuatBan { get; set; }

    public string? NgonNgu { get; set; }

    [Range(1, 10000, ErrorMessage = "Số trang phải lớn hơn 0")]
    public int? SoTrang { get; set; }

    public string? MoTa { get; set; }

    [Range(0, 10000)]
    public int SoLuongKho { get; set; }

    [Range(0, 10000)]
    public int SoLuongKhaDung { get; set; }

    public string? AnhBia { get; set; }
    public IFormFile? AnhBiaFile { get; set; }

    public List<string> SelectedTacGias { get; set; } = new();
}