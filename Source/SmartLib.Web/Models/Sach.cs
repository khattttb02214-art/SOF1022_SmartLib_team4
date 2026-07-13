using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("Sach")]
public class Sach
{
    [Key][StringLength(10)]
    public string MaSach { get; set; } = null!;
    [StringLength(20)]
    public string? ISBN { get; set; }
    [StringLength(100)]
    public string? Barcode { get; set; }
    [Required][StringLength(200)]
    public string TenSach { get; set; } = null!;
    [StringLength(10)]
    public string? MaTheLoai { get; set; }
    [StringLength(10)]
    public string? MaNXB { get; set; }
    public int? NamXuatBan { get; set; }
    [StringLength(50)]
    public string? NgonNgu { get; set; }
    public int? SoTrang { get; set; }
    public string? MoTa { get; set; }
    [StringLength(10)]
    public string? MaKe { get; set; }
    public int SoLuongKho { get; set; }
    public int SoLuongKhaDung { get; set; }
    public string? AnhBia { get; set; }
    public bool TrangThai { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    [ForeignKey(nameof(MaTheLoai))]
    public virtual TheLoai? TheLoai { get; set; }
    [ForeignKey(nameof(MaNXB))]
    public virtual NhaXuatBan? NhaXuatBan { get; set; }
    [ForeignKey(nameof(MaKe))]
    public virtual KeSach? KeSach { get; set; }
    public virtual ICollection<Sach_TacGia> SachTacGias { get; set; } = new List<Sach_TacGia>();
    public virtual ICollection<CuonSach> CuonSaches { get; set; } = new List<CuonSach>();
}
