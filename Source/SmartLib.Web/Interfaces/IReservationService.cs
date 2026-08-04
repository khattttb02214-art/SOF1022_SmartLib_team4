using SmartLib.Web.Models;

namespace SmartLib.Web.Interfaces;

/// <summary>
/// Các thao tác ĐỌC dữ liệu Đặt trước (Reservation, ChiTietDatTruoc) phục vụ AI Assistant.
/// AIService chỉ được phép lấy dữ liệu Đặt trước thông qua interface này, tuyệt đối không tự
/// truy vấn DbContext trực tiếp.
/// </summary>
public interface IReservationService
{
    /// <summary>
    /// Các đặt trước đang hoạt động (chưa hủy và chưa chuyển thành phiếu mượn) của 1 độc giả,
    /// kèm chi tiết từng cuốn sách đã đặt.
    /// </summary>
    Task<List<Reservation>> LayDatTruocDangHoatDongAsync(string maDocGia);

    /// <summary>Đếm tổng số đặt trước đang hoạt động trên toàn hệ thống (chưa hủy, chưa chuyển thành phiếu mượn).</summary>
    Task<int> DemDatTruocDangHoatDongAsync();
}
