using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Interfaces;
using SmartLib.Web.Models;

namespace SmartLib.Web.Services;

/// <summary>
/// Cài đặt IReservationService — lớp DUY NHẤT (ngoài các Controller đã có sẵn từ trước) được
/// phép truy vấn DbContext cho dữ liệu Đặt trước khi phục vụ AI Assistant.
///
/// Quy ước "đang hoạt động": TrangThai khác "Đã Hủy" VÀ DaMuon == false (chưa được chuyển
/// thành phiếu mượn thật) — khớp với 3 trạng thái đang dùng trong ReservationController:
/// "Đang Chờ", "Đã Duyệt", "Đã Hủy".
/// </summary>
public class ReservationService : IReservationService
{
    private readonly SmartLibDbContext _db;

    public ReservationService(SmartLibDbContext db) => _db = db;

    public async Task<List<Reservation>> LayDatTruocDangHoatDongAsync(string maDocGia)
    {
        if (string.IsNullOrEmpty(maDocGia)) return new List<Reservation>();

        return await _db.Reservations
            .Include(r => r.ChiTietDatTruocs)
                .ThenInclude(ct => ct.Sach)
            .Where(r => r.MaDocGia == maDocGia
                     && r.TrangThai != "Đã Hủy"
                     && !r.DaMuon)
            .OrderByDescending(r => r.NgayDat)
            .ToListAsync();
    }

    public async Task<int> DemDatTruocDangHoatDongAsync()
    {
        return await _db.Reservations.CountAsync(r => r.TrangThai != "Đã Hủy" && !r.DaMuon);
    }
}
