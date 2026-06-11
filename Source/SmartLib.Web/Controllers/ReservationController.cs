using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB,STU")]
public class ReservationController : Controller
{
    private readonly SmartLibDbContext _db;
    public ReservationController(SmartLibDbContext db) => _db = db;

    // ── Sync SoLuongKhaDung ───────────────────────────────────────────────────
    private async Task SyncSoLuongKhaDung(string? maSach)
    {
        if (string.IsNullOrEmpty(maSach)) return;
        var sach = await _db.Saches.Include(s => s.CuonSaches).FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach == null) return;
        sach.SoLuongKhaDung = sach.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
    }

    // ── Auto-gen MaPhieu ──────────────────────────────────────────────────────
    private async Task<string> GenerateMaPhieu()
    {
        string ma = "PM" + DateTime.Now.Ticks.ToString()[^8..];
        while (await _db.MuonTras.AnyAsync(m => m.MaPhieu == ma))
            ma = "PM" + (DateTime.Now.Ticks + new Random().Next(100)).ToString()[^8..];
        return ma;
    }

    // ── INDEX ─────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var q = _db.Reservations
            .Include(r => r.Sach)
            .Include(r => r.DocGia)
            .AsQueryable();

        if (User.IsInRole("STU"))
        {
            var maDocGia = User.FindFirst("MaDocGia")?.Value;
            if (!string.IsNullOrEmpty(maDocGia))
                q = q.Where(r => r.MaDocGia == maDocGia);
        }

        if (!string.IsNullOrEmpty(search))
            q = q.Where(r => r.Sach!.TenSach.Contains(search)
                          || (r.DocGia != null && r.DocGia.HoTen.Contains(search)));
        if (!string.IsNullOrEmpty(status))
            q = q.Where(r => r.TrangThai == status);

        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(await q.OrderByDescending(r => r.NgayDat).ToListAsync());
    }

    // ── CREATE (ADMIN/LIB tạo hộ) ────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Create()
    {
        ViewBag.DocGia = new SelectList(
            await _db.DocGias.Where(d => d.TrangThaiThe && d.DaDuyet).OrderBy(d => d.HoTen).ToListAsync(),
            "MaDocGia", "HoTen");
        return View();
    }

    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string MaDocGia, string MaSach)
    {
        if (await _db.Reservations.AnyAsync(r =>
                r.MaDocGia == MaDocGia && r.MaSach == MaSach && r.TrangThai == "Đang Chờ"))
        {
            TempData["error"] = "Độc giả này đã đặt trước sách đó rồi";
            return RedirectToAction(nameof(Index));
        }
        _db.Reservations.Add(new Reservation
        {
            MaDocGia = MaDocGia,
            MaSach = MaSach,
            NgayDat = DateTime.Now,
            TrangThai = "Đang Chờ",
            DaMuon = false
        });
        await _db.SaveChangesAsync();
        TempData["success"] = "Tạo đặt trước thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── API: tìm kiếm sách cho Create ────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    [HttpGet]
    public async Task<IActionResult> SearchSach(string? q)
    {
        var queryable = _db.Saches
            .Include(s => s.CuonSaches)
            .Where(s => s.TrangThai)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim();
            queryable = queryable.Where(s =>
                s.TenSach.Contains(kw) ||
                s.MaSach.Contains(kw) ||
                (s.ISBN != null && s.ISBN.Contains(kw)));
        }

        var sachList = await queryable
            .OrderBy(s => s.TenSach)
            .Take(30)
            .ToListAsync();

        // Dùng CuonSach làm nguồn sự thật
        var result = sachList.Select(s => new
        {
            maSach = s.MaSach,
            tenSach = s.TenSach,
            soLuongCoSan = s.CuonSaches.Count(c => c.TrangThai == "Có Sẵn"),
            coSan = s.CuonSaches.Any(c => c.TrangThai == "Có Sẵn")
        }).ToList();

        return Json(result);
    }

    // ── STU tự đặt ───────────────────────────────────────────────────────────
    [Authorize(Roles = "STU")]
    public async Task<IActionResult> StuCreate(string id)
    {
        var maDocGia = User.FindFirst("MaDocGia")?.Value;
        if (string.IsNullOrEmpty(maDocGia))
        {
            TempData["error"] = "Không tìm thấy tài khoản";
            return RedirectToAction("Index", "Home");
        }
        if (await _db.Reservations.AnyAsync(r =>
                r.MaDocGia == maDocGia && r.MaSach == id && r.TrangThai == "Đang Chờ"))
        {
            TempData["error"] = "Bạn đã đặt trước sách này rồi";
            return RedirectToAction(nameof(Index));
        }
        _db.Reservations.Add(new Reservation
        {
            MaDocGia = maDocGia,
            MaSach = id,
            NgayDat = DateTime.Now,
            TrangThai = "Đang Chờ",
            DaMuon = false
        });
        await _db.SaveChangesAsync();
        TempData["success"] = "Đặt trước thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── EDIT trạng thái ───────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Edit(int id)
    {
        var r = await _db.Reservations.FindAsync(id);
        if (r == null) return NotFound();
        ViewBag.StatusList = new SelectList(new[] { "Đang Chờ", "Đã Duyệt", "Đã Hủy" });
        return View(r);
    }

    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string TrangThai)
    {
        var r = await _db.Reservations.FindAsync(id);
        if (r == null) return NotFound();
        r.TrangThai = TrangThai;
        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật đặt trước thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── APPROVE ───────────────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await _db.Reservations.FindAsync(id);
        if (r == null) return NotFound();

        r.TrangThai = "Đã Duyệt";
        r.DaMuon = false;
        await _db.SaveChangesAsync();

        TempData["success"] = "Đã duyệt đặt trước. Bấm \"Lập phiếu mượn\" khi độc giả đến nhận sách.";
        return RedirectToAction(nameof(Index));
    }

    // ── BORROW: lập phiếu mượn từ đặt trước ─────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> CreateBorrow(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.Sach)
            .Include(x => x.DocGia)
            .FirstOrDefaultAsync(x => x.MaReservation == id);

        if (r == null) return NotFound();

        if (r.TrangThai != "Đã Duyệt")
        {
            TempData["error"] = "Chỉ có thể lập phiếu mượn cho đặt trước đã được duyệt";
            return RedirectToAction(nameof(Index));
        }
        if (r.DaMuon)
        {
            TempData["warning"] = $"Đặt trước này đã có phiếu mượn: {r.MaPhieuMuon}";
            return RedirectToAction(nameof(Index));
        }

        // Tìm cuốn sách còn sẵn
        var cuon = await _db.CuonSaches
            .Where(c => c.MaSach == r.MaSach && c.TrangThai == "Có Sẵn")
            .FirstOrDefaultAsync();

        if (cuon == null)
        {
            TempData["error"] = $"Không còn cuốn nào của sách «{r.Sach?.TenSach}» đang sẵn có. Vui lòng kiểm tra kho.";
            return RedirectToAction(nameof(Index));
        }

        var maNV = User.FindFirst("MaNV")?.Value ?? "NV001";
        var maPhieu = await GenerateMaPhieu();

        _db.MuonTras.Add(new MuonTra
        {
            MaPhieu = maPhieu,
            MaDocGia = r.MaDocGia,
            MaNV = maNV,
            NgayMuon = DateTime.Now,
            NgayHenTra = DateTime.Now.AddDays(14),
            TrangThai = "Đang Mượn",
            TienPhat = 0,
            GhiChu = $"Tạo từ đặt trước #{r.MaReservation}"
        });

        cuon.TrangThai = "Đang Mượn";

        _db.ChiTietMuonTras.Add(new ChiTietMuonTra
        {
            MaPhieu = maPhieu,
            MaCuonSach = cuon.MaCuonSach,
            MaSach = cuon.MaSach,
            SoLuong = 1,
            TienPhat = 0
        });

        r.DaMuon = true;
        r.MaPhieuMuon = maPhieu;

        await _db.SaveChangesAsync();

        // Sync SoLuongKhaDung
        await SyncSoLuongKhaDung(r.MaSach);
        await _db.SaveChangesAsync();

        TempData["success"] =
            $"Đã lập phiếu mượn {maPhieu} cho {r.DocGia?.HoTen} – Sách: «{r.Sach?.TenSach}». Hạn trả: {DateTime.Now.AddDays(14):dd/MM/yyyy}.";
        return RedirectToAction(nameof(Index));
    }

    // ── CANCEL ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Cancel(int id)
    {
        var r = await _db.Reservations.FindAsync(id);
        if (r == null) return NotFound();
        r.TrangThai = "Đã Hủy";
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã hủy đặt trước";
        return RedirectToAction(nameof(Index));
    }

    // ── DELETE ────────────────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _db.Reservations.FindAsync(id);
        if (r == null) return NotFound();
        _db.Reservations.Remove(r);
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã xóa đặt trước";
        return RedirectToAction(nameof(Index));
    }
}