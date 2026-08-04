using SmartLib.Web.Models;

namespace SmartLib.Web.ViewModels;

/// <summary>
/// Dữ liệu cho màn "Quản lý Wishlist" phía Admin/Thủ thư: tổng hợp wishlist của
/// toàn bộ sinh viên để nắm nhu cầu đọc và chủ động gợi ý sách.
/// </summary>
public class AdminWishlistViewModel
{
    // ── KPI tổng quan ─────────────────────────────────────────────
    public int TongLuotYeuThich { get; set; }
    public int TongSachDuocQuanTam { get; set; }
    public int TongSinhVienThamGia { get; set; }
    public int TongGoiYDaGui { get; set; }

    // ── Top sách được yêu thích nhiều nhất (để cân nhắc nhập thêm / gợi ý) ──
    public List<SachYeuThichNhieuDto> TopSachYeuThich { get; set; } = new();

    // ── Thống kê sở thích phổ biến theo Thể loại / Tác giả / NXB ──
    public List<ThongKeSoThichDto> TopTheLoai { get; set; } = new();
    public List<ThongKeSoThichDto> TopTacGia { get; set; } = new();
    public List<ThongKeSoThichDto> TopNXB { get; set; } = new();

    // ── Wishlist theo từng sinh viên ──────────────────────────────
    public List<SinhVienWishlistDto> DanhSachSinhVien { get; set; } = new();
    public string? SearchSinhVien { get; set; }

    // ── Lịch sử gợi ý đã gửi (mới nhất trước) ─────────────────────
    public List<ThongBao> LichSuGoiY { get; set; } = new();
}

/// <summary>Một sách trong bảng xếp hạng "được yêu thích nhiều nhất".</summary>
public class SachYeuThichNhieuDto
{
    public Sach Sach { get; set; } = null!;
    public int SoLuotYeuThich { get; set; }

    /// <summary>Hết hàng (SoLuongKhaDung == 0) nhưng vẫn đang được nhiều SV chờ đợi → cần lưu ý nhập thêm.</summary>
    public bool CanNhapThem => Sach.SoLuongKhaDung <= 0 && Sach.SoLuongKho >= 0;
}

/// <summary>Thống kê 1 giá trị sở thích (1 thể loại / 1 tác giả / 1 NXB) và số lượt theo dõi.</summary>
public class ThongKeSoThichDto
{
    public string MaRef { get; set; } = "";
    public string Ten { get; set; } = "";
    public int SoLuotTheoDoi { get; set; }
}

/// <summary>Tổng hợp hoạt động wishlist của 1 sinh viên, dùng cho bảng "Theo sinh viên".</summary>
public class SinhVienWishlistDto
{
    public DocGia DocGia { get; set; } = null!;
    public int SoSachYeuThich { get; set; }
    public int SoSoThich { get; set; }
    public DateTime? LanCuoiThem { get; set; }
}

/// <summary>Payload gửi lên khi thủ thư/admin bấm "Gửi gợi ý" trong modal.</summary>
public class GuiGoiYRequest
{
    public string MaSach { get; set; } = "";
    public List<string> MaDocGias { get; set; } = new();
    public string? LoiNhan { get; set; }
}
