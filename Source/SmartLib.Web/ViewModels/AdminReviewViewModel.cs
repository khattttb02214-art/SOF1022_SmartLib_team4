using SmartLib.Web.Models;

namespace SmartLib.Web.ViewModels;

/// <summary>Dữ liệu cho màn "Quản lý đánh giá sách" phía Admin/Thủ thư.</summary>
public class AdminReviewViewModel
{
    public List<DanhGiaSach> DanhSach { get; set; } = new();

    // ── Bộ lọc (giữ lại giá trị khi submit form GET) ──────────────
    public string? Search { get; set; }
    public int? SoSao { get; set; }
    public string? TrangThai { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }

    // ── KPI tổng quan (tính trên TOÀN BỘ dữ liệu, không phụ thuộc bộ lọc hiện tại) ──
    public int TongSoDanhGia { get; set; }
    public int SoDangHienThi { get; set; }
    public int SoDaXoa { get; set; }
    public double DiemTrungBinh { get; set; }
}
