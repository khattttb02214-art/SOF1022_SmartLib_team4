using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("NhanVien")]
public class NhanVien
{
    [Key][StringLength(10)]
    public string MaNV { get; set; } = null!;

    [Required][StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [StringLength(255)]
    public string? DiaChi { get; set; }

    [Required][StringLength(255)]
    public string MatKhau { get; set; } = null!;

    [StringLength(255)]
    public string? AnhDaiDien { get; set; }

    [StringLength(10)]
    public string? MaChucVu { get; set; }

    public bool TrangThai { get; set; } = true;

    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayCapNhat { get; set; }

    // Link sang DocGia (cho tài khoản STU)
    [StringLength(10)]
    public string? MaDocGia { get; set; }

    // OTP xác nhận email
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }

    // Trạng thái xác minh email (bắt buộc với tài khoản STU tự đăng ký)
    public bool EmailVerified { get; set; } = true;

    // Email mới đang chờ xác nhận (khi sinh viên đổi email)
    public string? PendingEmail { get; set; }

    [ForeignKey("MaChucVu")]
    public virtual ChucVu? ChucVu { get; set; }

    public virtual ICollection<MuonTra> MuonTras { get; set; } = new List<MuonTra>();
    public virtual ICollection<NhatKyHoatDong> NhatKyHoatDongs { get; set; } = new List<NhatKyHoatDong>();
}
