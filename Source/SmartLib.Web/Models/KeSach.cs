using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLib.Web.Models;

[Table("KeSach")]
public class KeSach
{
    [Key][StringLength(10)]
    public string MaKe { get; set; } = null!;

    [Required][StringLength(100)]
    public string TenKe { get; set; } = null!;

    [StringLength(200)]
    public string? ViTri { get; set; }

    /// <summary>Tầng trong thư viện (1, 2, 3…)</summary>
    public int? Tang { get; set; }

    /// <summary>Phòng / khu vực (VD: Phòng A, Khu KHTN…)</summary>
    [StringLength(50)]
    public string? Phong { get; set; }

    /// <summary>Mô tả thêm về kệ</summary>
    [StringLength(200)]
    public string? MoTa { get; set; }

    /// <summary>Sức chứa tối đa (số cuốn)</summary>
    public int? SucChua { get; set; }

    /// <summary>Nhà xuất bản phụ trách (kệ chuyên theo NXB)</summary>
    [StringLength(10)]
    public string? MaNXBPhuTrach { get; set; }

    /// <summary>Thể loại phụ trách (kệ chuyên theo thể loại)</summary>
    [StringLength(10)]
    public string? MaTheLoaiPhuTrach { get; set; }

    public bool TrangThai { get; set; } = true;

    [ForeignKey(nameof(MaNXBPhuTrach))]
    public virtual NhaXuatBan? NXBPhuTrach { get; set; }

    [ForeignKey(nameof(MaTheLoaiPhuTrach))]
    public virtual TheLoai? TheLoaiPhuTrach { get; set; }

    public virtual ICollection<Sach> Saches { get; set; } = new List<Sach>();
}