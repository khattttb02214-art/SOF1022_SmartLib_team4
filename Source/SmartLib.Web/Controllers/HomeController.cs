using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

public class HomeController : Controller
{
    private readonly SmartLibDbContext _db;
    public HomeController(SmartLibDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("STU"))
            return RedirectToAction("Index","Student");
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index","Staff");

        var model = new PublicHomeViewModel
        {
            TongSach    = await _db.Saches.CountAsync(s=>s.TrangThai),
            TongTheLoai = await _db.TheLoais.CountAsync(),
            SachKhaDung = await _db.Saches.CountAsync(s=>s.SoLuongKhaDung>0&&s.TrangThai),
            SachMoiNhat = await _db.Saches.Include(s=>s.TheLoai)
                .Where(s=>s.TrangThai)
                .OrderByDescending(s=>s.NgayTao).Take(8)
                .Select(s=>new SachMoiItem {
                    MaSach=s.MaSach, TenSach=s.TenSach,
                    TenTheLoai=s.TheLoai!=null?s.TheLoai.TenTheLoai:"Chưa phân loại",
                    AnhBia=s.AnhBia, SoLuongKhaDung=s.SoLuongKhaDung
                }).ToListAsync(),
            DanhSachTheLoai = await _db.TheLoais
                .Select(t=>new TheLoaiItem {
                    MaTheLoai=t.MaTheLoai, TenTheLoai=t.TenTheLoai,
                    SoLuongSach=_db.Saches.Count(s=>s.MaTheLoai==t.MaTheLoai)
                }).ToListAsync()
        };
        return View(model);
    }

    // Tìm kiếm công khai
    public async Task<IActionResult> Search(string? q, string? theLoai)
    {
        var query = _db.Saches.Include(s=>s.TheLoai).Include(s=>s.NhaXuatBan)
            .Include(s=>s.SachTacGias).ThenInclude(st=>st.TacGia)
            .Where(s=>s.TrangThai).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.TenSach.Contains(q) || (s.ISBN!=null&&s.ISBN.Contains(q))
                || s.SachTacGias.Any(st=>st.TacGia!=null&&st.TacGia.TenTacGia.Contains(q)));
        if (!string.IsNullOrEmpty(theLoai))
            query = query.Where(s=>s.MaTheLoai==theLoai);

        ViewBag.Query   = q;
        ViewBag.TheLoai = theLoai;
        ViewBag.TheLoais = await _db.TheLoais.ToListAsync();
        return View(await query.OrderByDescending(s=>s.NgayTao).ToListAsync());
    }
    [HttpGet]
    public async Task<IActionResult> SearchSuggest(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new List<object>());

        var result = await _db.Saches
            .Include(s => s.TheLoai)
            .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
            .Where(s => s.TrangThai &&
                (s.TenSach.Contains(q)
                 || (s.ISBN != null && s.ISBN.Contains(q))
                 || s.SachTacGias.Any(st => st.TacGia != null && st.TacGia.TenTacGia.Contains(q))))
            .OrderByDescending(s => s.NgayTao)
            .Take(6)
            .Select(s => new
            {
                maSach = s.MaSach,
                tenSach = s.TenSach,
                tenTacGia = s.SachTacGias
                              .Where(st => st.TacGia != null)
                              .Select(st => st.TacGia!.TenTacGia)
                              .FirstOrDefault() ?? "",
                tenTheLoai = s.TheLoai != null ? s.TheLoai.TenTheLoai : "",
                anhBia = s.AnhBia
            })
            .ToListAsync();

        return Json(result);
    }
}
