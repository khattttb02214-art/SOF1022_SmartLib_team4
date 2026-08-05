using SmartLib.Web.Models;

namespace SmartLib.Web.ViewModels;

// ── Dùng cho Staff/Index (Admin & Thủ thư) ──────────────────
public class HomeViewModel
{
    public int TongSach { get; set; }
    public int TongDocGia { get; set; }
    public int TongNhanVien { get; set; }
    public int SachDangMuon { get; set; }
    public int SachQuaHan { get; set; }

    // ── Dùng riêng cho banner + khối "Thống kê" của Staff Dashboard ──
    public int DocGiaDaDuyet { get; set; }     // Độc giả đã được duyệt tài khoản (DaDuyet = true)
    public int SachCoSan { get; set; }         // Tổng số cuốn (bản sao) đang "Có Sẵn", có thể cho mượn
    public int DonDatTruocChoDuyet { get; set; } // Số đơn đặt trước đang ở trạng thái "Đang Chờ"

    public List<SachMoiItem> SachMoiNhat { get; set; } = new();
    public List<TheLoaiItem> DanhSachTheLoai { get; set; } = new();
    public List<MuonTraGanDayItem> MuonTraGanDay { get; set; } = new();

    // ── 2 danh sách hiển thị trực tiếp trên Staff Dashboard ──
    public List<PhieuQuaHanItem> PhieuMuonQuaHan { get; set; } = new();
    public List<DatTruocChoDuyetItem> DonDatTruocCanDuyet { get; set; } = new();
}

// ── Dùng cho Home/Index (Trang công khai) ───────────────────
public class PublicHomeViewModel
{
    public int TongSach { get; set; }
    public int TongTheLoai { get; set; }
    public int SachKhaDung { get; set; }

    public List<SachMoiItem> SachMoiNhat { get; set; } = new();
    public List<TheLoaiItem> DanhSachTheLoai { get; set; } = new();
}

// ── Dùng cho Student/Index ───────────────────────────────────
public class StudentHomeViewModel
{
    public int TongSach { get; set; }
    public int TongTheLoai { get; set; }
    public int SachKhaDung { get; set; }

    public List<SachMoiItem> SachMoiNhat { get; set; } = new();
    public List<TheLoaiItem> DanhSachTheLoai { get; set; } = new();
    public List<MuonTra> PhieuDangMuon { get; set; } = new();
}

// ── Shared items ─────────────────────────────────────────────
public class SachMoiItem
{
    public string MaSach { get; set; } = null!;
    public string TenSach { get; set; } = null!;
    public string? TenTheLoai { get; set; }
    public string? AnhBia { get; set; }
    public int SoLuongKhaDung { get; set; }
}

public class TheLoaiItem
{
    public string MaTheLoai { get; set; } = null!;
    public string TenTheLoai { get; set; } = null!;
    public int SoLuongSach { get; set; }
}

public class MuonTraGanDayItem
{
    public string MaPhieu { get; set; } = null!;
    public string TenDocGia { get; set; } = null!;
    public DateTime NgayMuon { get; set; }
    public DateTime NgayHenTra { get; set; }
    public string TrangThai { get; set; } = null!;
}

// ── Dùng cho khối "Phiếu mượn quá hạn" trên Staff Dashboard ──
public class PhieuQuaHanItem
{
    public string MaPhieu { get; set; } = null!;
    public string TenDocGia { get; set; } = null!;
    public string TenSachHienThi { get; set; } = null!;
    public DateTime NgayHenTra { get; set; }
    public int SoNgayQuaHan { get; set; }
}

// ── Dùng cho khối "Đơn đặt trước chờ duyệt" trên Staff Dashboard ──
public class DatTruocChoDuyetItem
{
    public int MaReservation { get; set; }
    public string TenDocGia { get; set; } = null!;
    public string TenSachHienThi { get; set; } = null!;
    public DateTime NgayDat { get; set; }
    public string TrangThai { get; set; } = null!;
}
