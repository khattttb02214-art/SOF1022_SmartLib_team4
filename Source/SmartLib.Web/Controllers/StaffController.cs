using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class StaffController : Controller
{
    private readonly SmartLibDbContext _context;

    public StaffController(SmartLibDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel();

        model.TongSach = await _context.Saches.CountAsync();
        model.TongDocGia = await _context.DocGias.CountAsync();
        model.TongNhanVien = await _context.NhanViens.CountAsync();
        model.SachDangMuon = await _context.MuonTras
            .CountAsync(x => x.TrangThai == "Đang Mượn");
        model.SachQuaHan = await _context.MuonTras
            .CountAsync(x => x.TrangThai == "Đang Mượn" && x.NgayHenTra < DateTime.Now);

        model.SachMoiNhat = await _context.Saches
            .Include(s => s.TheLoai)
            .Where(s => s.TrangThai == true)
            .OrderByDescending(s => s.NgayTao)
            .Take(6)
            .Select(s => new SachMoiItem
            {
                MaSach = s.MaSach,
                TenSach = s.TenSach,
                TenTheLoai = s.TheLoai != null ? s.TheLoai.TenTheLoai : "Chưa phân loại",
                AnhBia = s.AnhBia,
                SoLuongKhaDung = s.SoLuongKhaDung
            })
            .ToListAsync();

        model.DanhSachTheLoai = await _context.TheLoais
            .Select(t => new TheLoaiItem
            {
                MaTheLoai = t.MaTheLoai,
                TenTheLoai = t.TenTheLoai,
                SoLuongSach = _context.Saches.Count(s => s.MaTheLoai == t.MaTheLoai)
            })
            .ToListAsync();

        model.MuonTraGanDay = await _context.MuonTras
            .Include(m => m.DocGia)
            .OrderByDescending(m => m.NgayMuon)
            .Take(5)
            .Select(m => new MuonTraGanDayItem
            {
                MaPhieu = m.MaPhieu,
                TenDocGia = m.DocGia != null ? m.DocGia.HoTen : "—",
                NgayMuon = m.NgayMuon,
                NgayHenTra = m.NgayHenTra,
                TrangThai = m.TrangThai ?? "—"
            })
            .ToListAsync();

        return View(model);
    }
}
