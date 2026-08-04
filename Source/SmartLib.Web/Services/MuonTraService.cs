using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Interfaces;
using SmartLib.Web.Models;

namespace SmartLib.Web.Services;

/// <summary>
/// Cài đặt IMuonTraService — lớp DUY NHẤT (ngoài các Controller đã có sẵn từ trước) được phép
/// truy vấn DbContext cho dữ liệu Mượn/Trả khi phục vụ AI Assistant.
///
/// Quy ước trạng thái (khớp với BorrowController đang chạy thật): MuonTra.TrangThai chỉ nhận
/// giá trị "Đang Mượn" hoặc "Đã Trả"; KHÔNG có trạng thái "Quá Hạn" riêng — quá hạn được suy ra
/// bằng công thức (TrangThai == "Đang Mượn" && NgayHenTra < DateTime.Now).
/// </summary>
public class MuonTraService : IMuonTraService
{
    private readonly SmartLibDbContext _db;

    public MuonTraService(SmartLibDbContext db) => _db = db;

    public async Task<List<MuonTra>> LayPhieuDangMuonAsync(string maDocGia)
    {
        if (string.IsNullOrEmpty(maDocGia)) return new List<MuonTra>();

        return await _db.MuonTras
            .Include(m => m.ChiTietMuonTras)
                .ThenInclude(ct => ct.Sach)
            .Where(m => m.MaDocGia == maDocGia && m.TrangThai == "Đang Mượn")
            .OrderBy(m => m.NgayHenTra)
            .ToListAsync();
    }

    public async Task<List<MuonTra>> LayPhieuQuaHanAsync(string maDocGia)
    {
        if (string.IsNullOrEmpty(maDocGia)) return new List<MuonTra>();

        return await _db.MuonTras
            .Include(m => m.ChiTietMuonTras)
                .ThenInclude(ct => ct.Sach)
            .Where(m => m.MaDocGia == maDocGia
                     && m.TrangThai == "Đang Mượn"
                     && m.NgayHenTra < DateTime.Now)
            .OrderBy(m => m.NgayHenTra)
            .ToListAsync();
    }

    public async Task<int> DemLuotMuonHomNayAsync()
    {
        var homNay = DateTime.Today;
        return await _db.MuonTras.CountAsync(m => m.NgayMuon.Date == homNay);
    }

    public async Task<int> DemPhieuQuaHanAsync()
    {
        return await _db.MuonTras.CountAsync(m =>
            m.TrangThai == "Đang Mượn" && m.NgayHenTra < DateTime.Now);
    }

    public async Task<SachMuonNhieuNhatDto?> LaySachMuonNhieuNhatAsync()
    {
        // Gộp toàn bộ ChiTietMuonTra theo MaSach, cộng dồn SoLuong để tìm quyển được mượn
        // nhiều nhất từ trước đến nay (không giới hạn khoảng thời gian).
        var top = await _db.ChiTietMuonTras
            .Where(ct => ct.MaSach != null)
            .GroupBy(ct => ct.MaSach)
            .Select(g => new { MaSach = g.Key!, Tong = g.Sum(x => x.SoLuong) })
            .OrderByDescending(x => x.Tong)
            .FirstOrDefaultAsync();

        if (top == null) return null;

        var sach = await _db.Saches.FirstOrDefaultAsync(s => s.MaSach == top.MaSach);
        if (sach == null) return null;

        return new SachMuonNhieuNhatDto(sach.MaSach, sach.TenSach, top.Tong);
    }
}
