using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("DocGia")]
public class DocGia
{
    [Key][StringLength(10)]
    public string MaDocGia { get; set; } = null!;

    [Required][StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(50)]
    public string? Lop { get; set; }

    [StringLength(100)]
    public string? Khoa { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    public string? DiaChi { get; set; }

    public DateTime? NgaySinh { get; set; }
    public DateTime? NgayTaoThe { get; set; }
    public DateTime? NgayHetHan { get; set; }

    public bool TrangThaiThe { get; set; }
    public string? AnhDaiDien { get; set; }

    // Mã thẻ thư viện — bắt buộc khi đăng ký
    [StringLength(20)]
    public string? MaTheTV { get; set; }

    // Trạng thái chờ duyệt tài khoản
    public bool DaDuyet { get; set; } = false;

    public virtual ICollection<MuonTra> MuonTras { get; set; } = new List<MuonTra>();
    public virtual ICollection<TheThuVien> TheThiViens { get; set; } = new List<TheThuVien>();
}
