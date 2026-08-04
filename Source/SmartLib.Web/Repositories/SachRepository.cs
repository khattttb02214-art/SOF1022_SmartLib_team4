using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Interfaces;
using SmartLib.Web.Models;

namespace SmartLib.Web.Repositories;

/// <summary>
/// Cài đặt ISachRepository — lớp DUY NHẤT thao tác trực tiếp với SmartLibDbContext cho dữ liệu
/// Sách. SachService (BookService) và các Service khác chỉ được gọi qua interface này, không
/// bao giờ tự truy vấn DbContext.
/// </summary>
public class SachRepository : ISachRepository
{
    private readonly SmartLibDbContext _db;

    public SachRepository(SmartLibDbContext db) => _db = db;

    // Include dùng chung cho các truy vấn cần đủ dữ liệu hiển thị (ảnh bìa, tên, thể loại,
    // NXB, tác giả) — tách thành 1 hàm để không lặp lại 3 dòng Include ở nhiều nơi.
    private IQueryable<Sach> TruyVanDayDu() => _db.Saches
        .Include(s => s.TheLoai)
        .Include(s => s.NhaXuatBan)
        .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
        .AsQueryable();

    public async Task<Sach?> LayTheoMaAsync(string maSach)
    {
        return await TruyVanDayDu().FirstOrDefaultAsync(s => s.MaSach == maSach);
    }

    public async Task<List<Sach>> TimTheoTuKhoaAsync(string tuKhoa, int soLuongToiDa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa)) return new List<Sach>();

        return await TruyVanDayDu()
            .Where(s => s.TrangThai && (
                s.TenSach.Contains(tuKhoa)
                || (s.TheLoai != null && s.TheLoai.TenTheLoai.Contains(tuKhoa))
                || s.SachTacGias.Any(st => st.TacGia != null && st.TacGia.TenTacGia.Contains(tuKhoa))
            ))
            .OrderByDescending(s => s.NgayTao)
            .Take(soLuongToiDa)
            .ToListAsync();
    }

    public async Task<List<(string MaSach, string TenSach)>> LayDanhSachTenSachAsync()
    {
        var raw = await _db.Saches
            .Where(s => s.TrangThai)
            .Select(s => new { s.MaSach, s.TenSach })
            .ToListAsync();

        return raw.Select(x => (x.MaSach, x.TenSach)).ToList();
    }

    public async Task<List<Sach>> LayTheoDanhSachMaAsync(List<string> maSachs)
    {
        if (maSachs == null || maSachs.Count == 0) return new List<Sach>();

        return await TruyVanDayDu()
            .Where(s => maSachs.Contains(s.MaSach))
            .ToListAsync();
    }
}
