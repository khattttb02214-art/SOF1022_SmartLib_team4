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
}
