using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB,STU")]
public class ReservationController : Controller
{
    private readonly SmartLibDbContext _db;
    public ReservationController(SmartLibDbContext db) => _db = db;

    // ── Sync SoLuongKhaDung ──────────────────────────────────────────────────────────────────────
    private async Task SyncSoLuongKhaDung(string? maSach)
    {
        if (string.IsNullOrEmpty(maSach)) return;
        var sach = await _db.Saches.Include(s => s.CuonSaches).FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach == null) return;
        sach.SoLuongKhaDung = sach.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
    }

    // ── Auto-gen MaPhieu ────────────────────────────────────────────────────────────────────────
    private async Task<string> GenerateMaPhieu()
    {
        string ma = "PM" + DateTime.Now.Ticks.ToString()[^8..];
        while (await _db.MuonTras.AnyAsync(m => m.MaPhieu == ma))
            ma = "PM" + (DateTime.Now.Ticks + new Random().Next(100)).ToString()[^8..];
        return ma;
    }

    // ── Find or Create same-day reservation ──────────────────────────────────────────────────────
    private async Task<Reservation?> FindOrCreateTodayReservation(string maDocGia)
    {
        var today = DateTime.Today;
        return await _db.Reservations
            .Where(r => r.MaDocGia == maDocGia && r.NgayDat.Date == today && r.TrangThai == "Đang Chờ")
            .FirstOrDefaultAsync();
    }

    // ── INDEX ────────────────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var q = _db.Reservations
            .Include(r => r.DocGia)
            .Include(r => r.ChiTietDatTruocs)
                .ThenInclude(ct => ct.Sach)
            .Include(r => r.NhanVien)
            .AsQueryable();

        if (User.IsInRole("STU"))
        {
            var maDocGia = User.FindFirst("MaDocGia")?.Value;
            if (!string.IsNullOrEmpty(maDocGia))
                q = q.Where(r => r.MaDocGia == maDocGia);
        }

        if (!string.IsNullOrEmpty(search))
            q = q.Where(r => (r.DocGia != null && r.DocGia.HoTen.Contains(search))
                          || r.ChiTietDatTruocs.Any(ct => ct.Sach != null && ct.Sach.TenSach.Contains(search)));

        if (!string.IsNullOrEmpty(status))
            q = q.Where(r => r.TrangThai == status);

        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(await q.OrderByDescending(r => r.NgayDat).ToListAsync());
    }

    // ── DETAILS ──────────────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.DocGia)
            .Include(x => x.NhanVien)
            .Include(x => x.ChiTietDatTruocs)
                .ThenInclude(x => x.Sach)
            .FirstOrDefaultAsync(x => x.MaReservation == id);

        if (r == null) return NotFound();
        return View(r);
    }

    // ── CREATE GET (ADMIN/LIB tạo hộ) ────────────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Create()
    {
        ViewBag.DocGia = new SelectList(
            await _db.DocGias.Where(d => d.TrangThaiThe && d.DaDuyet).OrderBy(d => d.HoTen).ToListAsync(),
            "MaDocGia", "HoTen");
        return View(new ReservationViewModel());
    }

    // ── CREATE POST ──────────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string MaDocGia, List<string>? SelectedBooks, string? GhiChu)
    {
        if (string.IsNullOrEmpty(MaDocGia))
        {
            TempData["error"] = "Vui lòng chọn độc giả";
            return RedirectToAction(nameof(Create));
        }

        if (SelectedBooks == null || !SelectedBooks.Any())
        {
            TempData["error"] = "Vui lòng chọn ít nhất 1 cuốn sách";
            return RedirectToAction(nameof(Create));
        }

        // Find or create same-day reservation
        var reservation = await FindOrCreateTodayReservation(MaDocGia);
        if (reservation == null)
        {
            reservation = new Reservation
            {
                MaDocGia = MaDocGia,
                NgayDat = DateTime.Now,
                TrangThai = "Đang Chờ",
                DaMuon = false,
                GhiChu = GhiChu
            };
            _db.Reservations.Add(reservation);
        }

        // Add books to reservation
        var existingBooks = await _db.ChiTietDatTruocs
            .Where(ct => ct.MaReservation == reservation.MaReservation)
            .Select(ct => ct.MaSach)
            .ToListAsync();

        foreach (var maSach in SelectedBooks)
        {
            if (existingBooks.Contains(maSach))
                continue;

            var sach = await _db.Saches.FirstOrDefaultAsync(s => s.MaSach == maSach);
            if (sach == null) continue;

            _db.ChiTietDatTruocs.Add(new ChiTietDatTruoc
            {
                MaReservation = reservation.MaReservation,
                MaSach = maSach,
                SoLuong = 1
            });
        }

        await _db.SaveChangesAsync();
        TempData["success"] = "Tạo đặt trước thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── EDIT GET ─────────────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Edit(int id)
    {
        var reservation = await _db.Reservations
            .Include(r => r.DocGia)
            .Include(r => r.ChiTietDatTruocs)
                .ThenInclude(ct => ct.Sach)
            .FirstOrDefaultAsync(r => r.MaReservation == id);

        if (reservation == null) return NotFound();

        var model = new ReservationViewModel
        {
            MaReservation = reservation.MaReservation,
            MaDocGia = reservation.MaDocGia,
            TrangThai = reservation.TrangThai,
            GhiChu = reservation.GhiChu,
            NgayDat = reservation.NgayDat,
            SelectedBooks = reservation.ChiTietDatTruocs.Select(ct => ct.MaSach).ToList(),
            ChiTietList = reservation.ChiTietDatTruocs.Select(ct => new ChiTietDatTruocViewModel
            {
                MaChiTiet = ct.MaChiTiet,
                MaSach = ct.MaSach,
                TenSach = ct.Sach?.TenSach ?? "",
                SoLuong = ct.SoLuong,
                GhiChu = ct.GhiChu
            }).ToList()
        };

        ViewBag.DocGia = new SelectList(
            await _db.DocGias.Where(d => d.TrangThaiThe && d.DaDuyet).OrderBy(d => d.HoTen).ToListAsync(),
            "MaDocGia", "HoTen", reservation.MaDocGia);
        ViewBag.StatusList = new SelectList(new[] { "Đang Chờ", "Đã Duyệt", "Đã Hủy" });

        return View(model);
    }

    // ── EDIT POST ────────────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string MaDocGia, List<string>? SelectedBooks, string? TrangThai, string? GhiChu)
    {
        var reservation = await _db.Reservations
            .Include(r => r.ChiTietDatTruocs)
            .FirstOrDefaultAsync(r => r.MaReservation == id);

        if (reservation == null) return NotFound();

        reservation.MaDocGia = MaDocGia;
        reservation.TrangThai = TrangThai ?? reservation.TrangThai;
        reservation.GhiChu = GhiChu;

        // Update books
        var existingBooks = reservation.ChiTietDatTruocs.Select(ct => ct.MaSach).ToHashSet();
        var newBooks = new HashSet<string>(SelectedBooks ?? new List<string>());

        // Remove books not in selection
        var booksToRemove = reservation.ChiTietDatTruocs.Where(ct => !newBooks.Contains(ct.MaSach)).ToList();
        foreach (var book in booksToRemove)
            _db.ChiTietDatTruocs.Remove(book);

        // Add new books
        foreach (var maSach in newBooks)
        {
            if (!existingBooks.Contains(maSach))
            {
                _db.ChiTietDatTruocs.Add(new ChiTietDatTruoc
                {
                    MaReservation = id,
                    MaSach = maSach,
                    SoLuong = 1
                });
            }
        }

        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật đặt trước thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── API: Search sách để tạo/sửa đặt trước ────────────────────────────────────────────────────
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

        var result = sachList.Select(s => new
        {
            maSach = s.MaSach,
            tenSach = s.TenSach,
            soLuongCoSan = s.CuonSaches.Count(c => c.TrangThai == "Có Sẵn"),
            coSan = s.CuonSaches.Any(c => c.TrangThai == "Có Sẵn")
        }).ToList();

        return Json(result);
    }

    // ── STU tự đặt ───────────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "STU")]
    public async Task<IActionResult> StuCreate(string id)
    {
        var maDocGia = User.FindFirst("MaDocGia")?.Value;
        if (string.IsNullOrEmpty(maDocGia))
        {
            TempData["error"] = "Không tìm thấy tài khoản";
            return RedirectToAction("Index", "Home");
        }

        // Find or create same-day reservation
        var reservation = await FindOrCreateTodayReservation(maDocGia);
        if (reservation == null)
        {
            reservation = new Reservation
            {
                MaDocGia = maDocGia,
                NgayDat = DateTime.Now,
                TrangThai = "Đang Chờ",
                DaMuon = false
            };
            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync();
        }

        // Check if already reserved
        if (await _db.ChiTietDatTruocs.AnyAsync(ct => ct.MaReservation == reservation.MaReservation && ct.MaSach == id))
        {
            TempData["error"] = "Bạn đã đặt trước sách này rồi";
            return RedirectToAction(nameof(Index));
        }

        _db.ChiTietDatTruocs.Add(new ChiTietDatTruoc
        {
            MaReservation = reservation.MaReservation,
            MaSach = id,
            SoLuong = 1
        });

        await _db.SaveChangesAsync();
        TempData["success"] = "Đặt trước thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── APPROVE ──────────────────────────────────────────────────────────────────────────────────
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

    // ── CREATE BORROW: lập phiếu mượn từ đặt trước ────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> CreateBorrow(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.DocGia)
            .Include(x => x.ChiTietDatTruocs)
                .ThenInclude(x => x.Sach)
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

        if (!r.ChiTietDatTruocs.Any())
        {
            TempData["error"] = "Đặt trước này không có sách nào";
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

        var affectedSach = new HashSet<string>();

        foreach (var item in r.ChiTietDatTruocs)
        {
            for (int i = 0; i < item.SoLuong; i++)
            {
                var cuon = await _db.CuonSaches
                    .FirstOrDefaultAsync(c => c.MaSach == item.MaSach && c.TrangThai == "Có Sẵn");

                if (cuon == null)
                {
                    TempData["error"] = $"Không đủ cuốn cho sách: {item.Sach?.TenSach}";
                    return RedirectToAction(nameof(Index));
                }

                cuon.TrangThai = "Đang Mượn";

                _db.ChiTietMuonTras.Add(new ChiTietMuonTra
                {
                    MaPhieu = maPhieu,
                    MaCuonSach = cuon.MaCuonSach,
                    MaSach = cuon.MaSach,
                    SoLuong = 1,
                    TienPhat = 0
                });

                affectedSach.Add(item.MaSach);
            }
        }

        r.DaMuon = true;
        r.MaPhieuMuon = maPhieu;

        await _db.SaveChangesAsync();

        foreach (var ma in affectedSach)
            await SyncSoLuongKhaDung(ma);
        await _db.SaveChangesAsync();

        var bookNames = string.Join(", ", r.ChiTietDatTruocs.Select(ct => $"«{ct.Sach?.TenSach}»"));
        TempData["success"] =
            $"Đã lập phiếu mượn {maPhieu} cho {r.DocGia?.HoTen} – Sách: {bookNames}. Hạn trả: {DateTime.Now.AddDays(14):dd/MM/yyyy}.";
        return RedirectToAction(nameof(Index));
    }

    // ── CANCEL ───────────────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Cancel(int id)
    {
        var r = await _db.Reservations.FindAsync(id);
        if (r == null) return NotFound();
        r.TrangThai = "Đã Hủy";
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã hủy đặt trước";
        return RedirectToAction(nameof(Index));
    }

    // ── DELETE ───────────────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _db.Reservations
            .Include(x => x.ChiTietDatTruocs)
            .FirstOrDefaultAsync(x => x.MaReservation == id);
        if (r == null) return NotFound();

        _db.ChiTietDatTruocs.RemoveRange(r.ChiTietDatTruocs);
        _db.Reservations.Remove(r);
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã xóa đặt trước";
        return RedirectToAction(nameof(Index));
    }
}
