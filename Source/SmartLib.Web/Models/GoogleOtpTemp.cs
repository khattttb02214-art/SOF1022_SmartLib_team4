using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

/// <summary>
/// Lưu OTP tạm thời cho luồng đăng ký bằng Google.
/// Xóa record này sau khi xác minh thành công hoặc hết hạn.
/// </summary>
[Table("GoogleOtpTemp")]
public class GoogleOtpTemp
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string GoogleId { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string HoTen { get; set; } = null!;

    [Required]
    [StringLength(6)]
    public string OtpCode { get; set; } = null!;

    public DateTime OtpExpiry { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}