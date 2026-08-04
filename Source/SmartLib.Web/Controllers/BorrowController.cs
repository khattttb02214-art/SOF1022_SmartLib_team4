using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.Services.Pdf;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class BorrowController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly BorrowReceiptPdfService _pdf;

    public BorrowController(SmartLibDbContext db, BorrowReceiptPdfService pdf)
    { _db = db; _pdf = pdf; }

    // ── Sync SoLuongKhaDung từ CuonSach (nguồn sự thật) ──────────────────────
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
        var q = _db.MuonTras.Include(x => x.DocGia).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.MaPhieu.Contains(search) || x.DocGia!.HoTen.Contains(search));

        if (!string.IsNullOrEmpty(status))
        {
            if (status == "OVERDUE")
                q = q.Where(x => x.TrangThai == "Đang Mượn" && x.NgayHenTra < DateTime.Now);
            else
                q = q.Where(x => x.TrangThai == status);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(await q.OrderByDescending(x => x.NgayMuon).ToListAsync());
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Details(string id)
    {
        var b = await _db.MuonTras
            .Include(x => x.DocGia)
            .Include(x => x.NhanVien)
            .Include(x => x.ChiTietMuonTras)
                .ThenInclude(x => x.Sach)
            .Include(x => x.ChiTietMuonTras)
                .ThenInclude(x => x.CuonSach)
            .FirstOrDefaultAsync(x => x.MaPhieu == id);
        
        if (b == null) return NotFound();
        return View(b);
    }    public async Task<IActionResult> Create()
    {
        await LoadForm();
        return View(new BorrowViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(BorrowViewModel model)
    {
        if (!ModelState.IsValid) { await LoadForm(); return View(model); }

        if (model.SelectedBooks == null || !model.SelectedBooks.Any())
        {
            ModelState.AddModelError("SelectedBooks", "Vui lòng chọn ít nhất 1 cuốn sách");
            await LoadForm();
            return View(model);
        }

        // ── Độc giả này đã có phiếu "Đang Mượn" tạo TRONG NGÀY HÔM NAY chưa? ──
        // Có thì gộp các cuốn vừa chọn vào phiếu đó, KHÔNG tách thành phiếu riêng.
        var phieuHomNay = await _db.MuonTras
            .Include(x => x.ChiTietMuonTras)
            .Where(x => x.MaDocGia == model.MaDocGia
                     && x.TrangThai == "Đang Mượn"
                     && x.NgayMuon.Date == DateTime.Today)
            .OrderByDescending(x => x.NgayMuon)
            .FirstOrDefaultAsync();

        if (phieuHomNay != null)
        {
            var daCoTrongPhieu = phieuHomNay.ChiTietMuonTras.Select(c => c.MaCuonSach).ToHashSet();
            var affectedSachGop = new HashSet<string>();
            int themVao = 0;

            foreach (var maCuon in model.SelectedBooks)
            {
                if (daCoTrongPhieu.Contains(maCuon)) continue; // cuốn này đã có sẵn trong phiếu, bỏ qua tránh trùng
                var cuon = await _db.CuonSaches.FirstOrDefaultAsync(c => c.MaCuonSach == maCuon);
                if (cuon == null || cuon.TrangThai != "Có Sẵn") continue;
                cuon.TrangThai = "Đang Mượn";
                if (!string.IsNullOrEmpty(cuon.MaSach)) affectedSachGop.Add(cuon.MaSach);
                _db.ChiTietMuonTras.Add(new ChiTietMuonTra
                {
                    MaPhieu = phieuHomNay.MaPhieu,
                    MaCuonSach = cuon.MaCuonSach,
                    MaSach = cuon.MaSach,
                    SoLuong = 1,
                    TienPhat = 0
                });
                themVao++;
            }

            if (!string.IsNullOrWhiteSpace(model.GhiChu))
                phieuHomNay.GhiChu = string.IsNullOrWhiteSpace(phieuHomNay.GhiChu)
                    ? model.GhiChu
                    : $"{phieuHomNay.GhiChu}; {model.GhiChu}";

            await _db.SaveChangesAsync();
            foreach (var ma in affectedSachGop) await SyncSoLuongKhaDung(ma);
            await _db.SaveChangesAsync();

            TempData["success"] = themVao > 0
                ? $"Độc giả đã có phiếu mượn hôm nay ({phieuHomNay.MaPhieu}) — đã gộp thêm {themVao} cuốn vào phiếu đó thay vì tạo phiếu mới."
                : $"Độc giả đã có phiếu mượn hôm nay ({phieuHomNay.MaPhieu}). Các cuốn vừa chọn đã có sẵn trong phiếu này rồi.";
            return RedirectToAction(nameof(Edit), new { id = phieuHomNay.MaPhieu });
        }

        // Auto-gen mã phiếu
        model.MaPhieu = await GenerateMaPhieu();

        var maNV = User.FindFirst("MaNV")?.Value ?? "NV001";

        var borrow = new MuonTra
        {
            MaPhieu = model.MaPhieu,
            MaDocGia = model.MaDocGia,
            MaNV = maNV,
            NgayMuon = DateTime.Now,
            NgayHenTra = DateTime.Now.AddDays(14),
            TrangThai = "Đang Mượn",
            TienPhat = 0,
            GhiChu = model.GhiChu
        };
        _db.MuonTras.Add(borrow);

        var affectedSach = new HashSet<string>();
        foreach (var maCuon in model.SelectedBooks)
        {
            var cuon = await _db.CuonSaches.FirstOrDefaultAsync(c => c.MaCuonSach == maCuon);
            if (cuon == null || cuon.TrangThai != "Có Sẵn") continue;
            cuon.TrangThai = "Đang Mượn";
            if (!string.IsNullOrEmpty(cuon.MaSach))
                affectedSach.Add(cuon.MaSach);
            _db.ChiTietMuonTras.Add(new ChiTietMuonTra
            {
                MaPhieu = model.MaPhieu,
                MaCuonSach = cuon.MaCuonSach,
                MaSach = cuon.MaSach,
                SoLuong = 1,
                TienPhat = 0
            });
        }

        await _db.SaveChangesAsync();

        // Sync SoLuongKhaDung sau khi lưu
        foreach (var ma in affectedSach)
            await SyncSoLuongKhaDung(ma);
        await _db.SaveChangesAsync();

        TempData["success"] = "Tạo phiếu mượn thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── EDIT ──────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Edit(string id)
    {
        var b = await _db.MuonTras
            .Include(x => x.DocGia)
            .Include(x => x.ChiTietMuonTras)
                .ThenInclude(ct => ct.Sach)
            .Include(x => x.ChiTietMuonTras)
                .ThenInclude(ct => ct.CuonSach)
            .FirstOrDefaultAsync(x => x.MaPhieu == id);

        if (b == null) return NotFound();

        ViewBag.CoTheThemSach = b.TrangThai == "Đang Mượn"
                                && b.NgayMuon.Date == DateTime.Today;
        return View(b);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, DateTime NgayHenTra, string? GhiChu)
    {
        var b = await _db.MuonTras.FindAsync(id);
        if (b == null) return NotFound();
        b.NgayHenTra = NgayHenTra;
        b.GhiChu = GhiChu;
        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật phiếu mượn thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── THÊM SÁCH VÀO PHIẾU ĐÃ TẠO (cùng ngày) ──────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBooksToPhieu(string id, List<string> NewBooks)
    {
        var b = await _db.MuonTras
            .Include(x => x.ChiTietMuonTras)
            .FirstOrDefaultAsync(x => x.MaPhieu == id);

        if (b == null) return NotFound();

        if (b.TrangThai != "Đang Mượn" || b.NgayMuon.Date != DateTime.Today)
        {
            TempData["error"] = "Chỉ có thể thêm sách vào phiếu được tạo trong ngày hôm nay";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (NewBooks == null || !NewBooks.Any())
        {
            TempData["error"] = "Vui lòng chọn ít nhất 1 cuốn sách để thêm";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var existingCuons = b.ChiTietMuonTras
            .Select(ct => ct.MaCuonSach)
            .ToHashSet();

        int added = 0;
        var affectedSach = new HashSet<string>();
        foreach (var maCuon in NewBooks)
        {
            if (existingCuons.Contains(maCuon)) continue;

            var cuon = await _db.CuonSaches.FirstOrDefaultAsync(c => c.MaCuonSach == maCuon);
            if (cuon == null || cuon.TrangThai != "Có Sẵn") continue;

            cuon.TrangThai = "Đang Mượn";
            if (!string.IsNullOrEmpty(cuon.MaSach))
                affectedSach.Add(cuon.MaSach);
            _db.ChiTietMuonTras.Add(new ChiTietMuonTra
            {
                MaPhieu = id,
                MaCuonSach = cuon.MaCuonSach,
                MaSach = cuon.MaSach,
                SoLuong = 1,
                TienPhat = 0
            });
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync();
            foreach (var ma in affectedSach)
                await SyncSoLuongKhaDung(ma);
            await _db.SaveChangesAsync();
            TempData["success"] = $"Đã thêm {added} cuốn sách vào phiếu mượn";
        }
        else
        {
            TempData["error"] = "Không có cuốn sách nào được thêm (đã tồn tại hoặc không còn sẵn)";
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── XÓA MỘT CUỐN KHỎI PHIẾU (cùng ngày) ─────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBookFromPhieu(string id, int chiTietId)
    {
        var b = await _db.MuonTras.FindAsync(id);
        if (b == null) return NotFound();

        if (b.TrangThai != "Đang Mượn" || b.NgayMuon.Date != DateTime.Today)
        {
            TempData["error"] = "Chỉ có thể xóa sách khỏi phiếu được tạo trong ngày hôm nay";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var chiTiet = await _db.ChiTietMuonTras
            .Include(ct => ct.CuonSach)
            .FirstOrDefaultAsync(ct => ct.Id == chiTietId && ct.MaPhieu == id);

        if (chiTiet == null)
        {
            TempData["error"] = "Không tìm thấy chi tiết mượn";
            return RedirectToAction(nameof(Edit), new { id });
        }

        string? maSach = chiTiet.CuonSach?.MaSach ?? chiTiet.MaSach;
        if (chiTiet.CuonSach != null)
            chiTiet.CuonSach.TrangThai = "Có Sẵn";

        _db.ChiTietMuonTras.Remove(chiTiet);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(maSach))
        {
            await SyncSoLuongKhaDung(maSach);
            await _db.SaveChangesAsync();
        }

        TempData["success"] = "Đã xóa cuốn sách khỏi phiếu mượn";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── DELETE ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Delete(string id)
    {
        var b = await _db.MuonTras
            .Include(x => x.ChiTietMuonTras)
            .FirstOrDefaultAsync(x => x.MaPhieu == id);
        if (b == null) return NotFound();
        if (b.TrangThai == "Đang Mượn")
        { TempData["error"] = "Không thể xóa phiếu đang mượn"; return RedirectToAction(nameof(Index)); }

        _db.ChiTietMuonTras.RemoveRange(b.ChiTietMuonTras);
        _db.MuonTras.Remove(b);
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã xóa phiếu mượn";
        return RedirectToAction(nameof(Index));
    }

    // ── TRẢ SÁCH ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> ReturnBook(string id)
    {
        var b = await _db.MuonTras
            .Include(x => x.ChiTietMuonTras)
            .FirstOrDefaultAsync(x => x.MaPhieu == id);
        if (b == null) return NotFound();

        b.NgayTraThucTe = DateTime.Now;
        b.TrangThai = "Đã Trả";
        decimal fine = 0;
        if (b.NgayTraThucTe > b.NgayHenTra)
            fine = (decimal)(b.NgayTraThucTe.Value.Date - b.NgayHenTra.Date).Days * 5000;
        b.TienPhat = fine;

        var affectedSach = new HashSet<string>();
        foreach (var item in b.ChiTietMuonTras)
        {
            var cuon = await _db.CuonSaches.FindAsync(item.MaCuonSach);
            if (cuon != null)
            {
                cuon.TrangThai = "Có Sẵn";
                if (!string.IsNullOrEmpty(cuon.MaSach))
                    affectedSach.Add(cuon.MaSach);
            }
        }
        await _db.SaveChangesAsync();

        // Sync SoLuongKhaDung
        foreach (var ma in affectedSach)
            await SyncSoLuongKhaDung(ma);
        await _db.SaveChangesAsync();

        TempData["success"] = fine > 0
            ? $"Trả sách thành công. Tiền phạt: {fine:N0} VNĐ"
            : "Trả sách thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── GIA HẠN ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Renew(string id)
    {
        var b = await _db.MuonTras.FindAsync(id);
        if (b == null) return NotFound();
        if (b.DaGiaHan)
        { TempData["error"] = "Phiếu này đã được gia hạn rồi"; return RedirectToAction(nameof(Index)); }
        b.NgayHenTra = b.NgayHenTra.AddDays(7);
        b.DaGiaHan = true;
        await _db.SaveChangesAsync();
        TempData["success"] = "Gia hạn thành công (+7 ngày)";
        return RedirectToAction(nameof(Index));
    }

    // ── EXPORT PDF ────────────────────────────────────────────────────────────
    public async Task<IActionResult> ExportPdf(string id)
    {
        var b = await _db.MuonTras
            .Include(x => x.DocGia)
            .Include(x => x.ChiTietMuonTras).ThenInclude(x => x.Sach)
            .FirstOrDefaultAsync(x => x.MaPhieu == id);
        if (b == null) return NotFound();
        var pdf = _pdf.Generate(b);
        return File(pdf, "application/pdf", $"PhieuMuon_{b.MaPhieu}.pdf");
    }

    // ── API: tìm sách → trả về cuốn sẵn có ─────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> SearchSachForBorrow(string? q)
    {
        var query = _db.Saches
            .Include(s => s.CuonSaches)
            .Where(s => s.TrangThai)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim();
            query = query.Where(s =>
                s.TenSach.Contains(kw) ||
                s.MaSach.Contains(kw) ||
                (s.ISBN != null && s.ISBN.Contains(kw)));
        }

        var sachList = await query
            .OrderBy(s => s.TenSach)
            .Take(30)
            .ToListAsync();

        var result = sachList.Select(s => new
        {
            maSach = s.MaSach,
            tenSach = s.TenSach,
            isbn = s.ISBN,
            soLuongCoSan = s.CuonSaches.Count(c => c.TrangThai == "Có Sẵn"),
            cuons = s.CuonSaches
                .Where(c => c.TrangThai == "Có Sẵn")
                .Select(c => new { c.MaCuonSach, c.Barcode })
                .ToList()
        }).ToList();

        return Json(result);
    }

    // ── API: tìm sách thêm vào phiếu (loại trừ cuốn đã có) ──────────────────
    [HttpGet]
    public async Task<IActionResult> SearchSachForEdit(string? q, string? maPhieu)
    {
        var existingCuons = string.IsNullOrEmpty(maPhieu)
            ? new HashSet<string>()
            : (await _db.ChiTietMuonTras
                .Where(ct => ct.MaPhieu == maPhieu && ct.MaCuonSach != null)
                .Select(ct => ct.MaCuonSach!)
                .ToListAsync())
              .ToHashSet();

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
            isbn = s.ISBN,
            soLuongCoSan = s.CuonSaches.Count(c => c.TrangThai == "Có Sẵn"),
            cuons = s.CuonSaches
                .Where(c => c.TrangThai == "Có Sẵn" && !existingCuons.Contains(c.MaCuonSach))
                .Select(c => new { c.MaCuonSach, c.Barcode })
                .ToList()
        }).ToList();

        return Json(result);
    }

    // ── API cũ (tương thích) ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAvailableBooks(string? search)
    {
        var q = _db.CuonSaches
            .Where(c => c.TrangThai == "Có Sẵn")
            .Include(c => c.Sach)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim();
            q = q.Where(c =>
                (c.Sach != null && c.Sach.TenSach.Contains(s)) ||
                c.MaCuonSach.Contains(s) ||
                (c.Barcode != null && c.Barcode.Contains(s)) ||
                (c.MaSach != null && c.MaSach.Contains(s)));
        }

        var data = await q.Take(50).Select(c => new
        {
            maCuonSach = c.MaCuonSach,
            tenSach = c.Sach != null ? c.Sach.TenSach : "(Không rõ)",
            maSach = c.MaSach,
            barcode = c.Barcode
        }).ToListAsync();

        return Json(data);
    }

    private async Task LoadForm()
    {
        ViewBag.DocGia = new SelectList(
            await _db.DocGias
                .Where(d => d.TrangThaiThe && d.DaDuyet)
                .OrderBy(d => d.HoTen)
                .ToListAsync(),
            "MaDocGia", "HoTen");
    }
}