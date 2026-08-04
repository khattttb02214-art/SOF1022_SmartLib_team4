using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class TacGiaViewModel
{
    [StringLength(10)]
    public string? MaTacGia { get; set; }

    [Required(ErrorMessage = "Tên tác giả không được trống")]
    [StringLength(100)]
    public string TenTacGia { get; set; } = null!;

    public string? TieuSu { get; set; }

    [StringLength(100)]
    public string? QuocTich { get; set; }

    [StringLength(100)]
    public string? ButDanh { get; set; }

    [Range(1, 2100, ErrorMessage = "Năm sinh không hợp lệ")]
    public int? NamSinh { get; set; }

    [Range(1, 2100, ErrorMessage = "Năm mất không hợp lệ")]
    public int? NamMat { get; set; }

    public string? AnhDaiDienHienTai { get; set; } // đường dẫn ảnh hiện có (khi Edit)
    public IFormFile? AnhDaiDien { get; set; }      // file ảnh mới upload lên (nếu có)
}
