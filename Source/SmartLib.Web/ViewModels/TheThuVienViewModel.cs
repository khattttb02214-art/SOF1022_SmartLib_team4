using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class TheThuVienViewModel
{
    public int? Id { get; set; }

    // Có thể để trống — controller sẽ tự sinh
    [StringLength(20)]
    public string? MaThe { get; set; }

    public string? MaDocGia { get; set; }

    [Required(ErrorMessage = "Ngày hết hạn không được trống")]
    public DateTime NgayHetHan { get; set; } = DateTime.Now.AddYears(4);

    public bool TrangThai { get; set; } = true;

    public IFormFile? AnhTheFile { get; set; }
}
