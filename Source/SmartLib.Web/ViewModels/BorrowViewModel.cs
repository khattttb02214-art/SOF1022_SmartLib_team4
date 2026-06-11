using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class BorrowViewModel
{
    public string? MaPhieu { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn độc giả")]
    public string MaDocGia { get; set; } = null!;

    public string? GhiChu { get; set; }

    // Danh sách cuốn sách được chọn (không giới hạn số lượng)
    // Không dùng [MinLength] trên List vì sẽ validate string length, không phải count
    public List<string> SelectedBooks { get; set; } = new();
}
