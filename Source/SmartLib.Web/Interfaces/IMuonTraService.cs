using SmartLib.Web.Models;

namespace SmartLib.Web.Interfaces;

/// <summary>Kết quả thống kê "sách được mượn nhiều nhất" (gộp theo tổng SoLuong trong ChiTietMuonTra).</summary>
public record SachMuonNhieuNhatDto(string MaSach, string TenSach, int TongLuotMuon);

/// <summary>
/// Các thao tác ĐỌC dữ liệu Mượn/Trả (MuonTra, ChiTietMuonTra) phục vụ AI Assistant.
/// AIService chỉ được phép lấy dữ liệu Mượn/Trả thông qua interface này, tuyệt đối không tự
/// truy vấn DbContext trực tiếp.
/// </summary>
public interface IMuonTraService
{
    /// <summary>Các phiếu đang mượn (chưa trả) của 1 độc giả, kèm chi tiết từng cuốn sách.</summary>
    Task<List<MuonTra>> LayPhieuDangMuonAsync(string maDocGia);

    /// <summary>Trong số các phiếu đang mượn của 1 độc giả, những phiếu nào đã quá hạn trả.</summary>
    Task<List<MuonTra>> LayPhieuQuaHanAsync(string maDocGia);

    /// <summary>Đếm tổng số phiếu mượn được lập trong ngày hôm nay (toàn hệ thống).</summary>
    Task<int> DemLuotMuonHomNayAsync();

    /// <summary>Đếm tổng số phiếu đang mượn nhưng đã quá hạn trả (toàn hệ thống).</summary>
    Task<int> DemPhieuQuaHanAsync();

    /// <summary>Sách được mượn nhiều lượt nhất từ trước đến nay; null nếu chưa có dữ liệu mượn nào.</summary>
    Task<SachMuonNhieuNhatDto?> LaySachMuonNhieuNhatAsync();
}
