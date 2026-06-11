using System.ComponentModel.DataAnnotations;

namespace SmartLib.Web.ViewModels;

public class NhaXuatBanViewModel
{
    [StringLength(10)]
    public string? MaNXB { get; set; }

    [Required(ErrorMessage = "Tên NXB không được trống")]
    [StringLength(100)]
    public string TenNXB { get; set; } = null!;

    [StringLength(255)]
    public string? DiaChi { get; set; }

    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }
}
