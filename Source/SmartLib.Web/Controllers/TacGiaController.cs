using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class TacGiaController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly IWebHostEnvironment _env;
    public TacGiaController(SmartLibDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    private async Task<string?> SaveAvatar(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        var name = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var folder = Path.Combine(_env.WebRootPath, "uploads/authors");
        Directory.CreateDirectory(folder);
        await using var fs = new FileStream(Path.Combine(folder, name), FileMode.Create);
        await file.CopyToAsync(fs);
        return name;
    }

    private void XoaAvatarCu(string? tenFile)
    {
        if (string.IsNullOrEmpty(tenFile)) return;
        var path = Path.Combine(_env.WebRootPath, "uploads/authors", tenFile);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }

    public async Task<IActionResult> Index(string? search)
    {
        var q = _db.TacGias.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(t => t.TenTacGia.Contains(search));
        ViewBag.Search = search;
        return View(await q.OrderBy(t => t.TenTacGia).ToListAsync());
    }

    public async Task<IActionResult> Detail(string id)
    {
        var tg = await _db.TacGias
            .Include(t => t.SachTacGias)
            .ThenInclude(st => st.Sach)
            .ThenInclude(s => s!.TheLoai)
            .FirstOrDefaultAsync(t => t.MaTacGia == id);
        if (tg == null) return NotFound();
        return View(tg);
    }

    public IActionResult Create() => View(new TacGiaViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TacGiaViewModel model)
    {
        if (model.NamSinh.HasValue && model.NamMat.HasValue && model.NamMat < model.NamSinh)
            ModelState.AddModelError("NamMat", "Năm mất phải sau năm sinh");

        if (!ModelState.IsValid) return View(model);

        // Auto-generate MaTacGia
        if (string.IsNullOrWhiteSpace(model.MaTacGia))
        {
            var last = await _db.TacGias.OrderByDescending(t => t.MaTacGia).Select(t => t.MaTacGia).FirstOrDefaultAsync();
            string newMa = "TG001";
            if (!string.IsNullOrEmpty(last) && last.StartsWith("TG") && int.TryParse(last[2..], out int n))
                newMa = "TG" + (n + 1).ToString("D3");
            model.MaTacGia = newMa;
        }

        if (await _db.TacGias.AnyAsync(t => t.MaTacGia == model.MaTacGia))
        { ModelState.AddModelError("MaTacGia", "Mã đã tồn tại"); return View(model); }

        var anhDaiDien = await SaveAvatar(model.AnhDaiDien);

        _db.TacGias.Add(new TacGia
        {
            MaTacGia = model.MaTacGia,
            TenTacGia = model.TenTacGia,
            TieuSu = model.TieuSu,
            QuocTich = model.QuocTich,
            ButDanh = model.ButDanh,
            NamSinh = model.NamSinh,
            NamMat = model.NamMat,
            AnhDaiDien = anhDaiDien
        });
        await _db.SaveChangesAsync();
        TempData["success"] = "Thêm tác giả thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var t = await _db.TacGias.FindAsync(id);
        if (t == null) return NotFound();
        return View(new TacGiaViewModel
        {
            MaTacGia = t.MaTacGia,
            TenTacGia = t.TenTacGia,
            TieuSu = t.TieuSu,
            QuocTich = t.QuocTich,
            ButDanh = t.ButDanh,
            NamSinh = t.NamSinh,
            NamMat = t.NamMat,
            AnhDaiDienHienTai = t.AnhDaiDien
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TacGiaViewModel model)
    {
        if (model.NamSinh.HasValue && model.NamMat.HasValue && model.NamMat < model.NamSinh)
            ModelState.AddModelError("NamMat", "Năm mất phải sau năm sinh");

        if (!ModelState.IsValid) return View(model);
        var t = await _db.TacGias.FindAsync(id);
        if (t == null) return NotFound();

        t.TenTacGia = model.TenTacGia;
        t.TieuSu = model.TieuSu;
        t.QuocTich = model.QuocTich;
        t.ButDanh = model.ButDanh;
        t.NamSinh = model.NamSinh;
        t.NamMat = model.NamMat;

        if (model.AnhDaiDien != null)
        {
            var anhMoi = await SaveAvatar(model.AnhDaiDien);
            XoaAvatarCu(t.AnhDaiDien);
            t.AnhDaiDien = anhMoi;
        }

        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật tác giả thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var t = await _db.TacGias.FindAsync(id);
        if (t == null) return NotFound();

        if (await _db.SachTacGias.AnyAsync(st => st.MaTacGia == id))
        {
            TempData["error"] = $"Không thể xóa: tác giả {t.TenTacGia} vẫn còn sách liên kết trong hệ thống. " +
                "Bạn có thể dùng nút \"Ngừng hoạt động\" thay vì xóa hẳn.";
            return RedirectToAction(nameof(Index));
        }

        _db.TacGias.Remove(t);
        await _db.SaveChangesAsync();
        XoaAvatarCu(t.AnhDaiDien);
        TempData["success"] = "Đã xóa tác giả";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ToggleStatus(string id)
    {
        var t = await _db.TacGias.FindAsync(id);
        if (t == null) return NotFound();
        t.TrangThai = !t.TrangThai;
        await _db.SaveChangesAsync();
        TempData["success"] = t.TrangThai ? "Đã kích hoạt lại tác giả" : "Đã ngừng hoạt động tác giả (giữ nguyên dữ liệu)";
        return RedirectToAction(nameof(Index));
    }

    // ── IMPORT EXCEL ─────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        { TempData["error"] = "Vui lòng chọn file Excel"; return RedirectToAction(nameof(Index)); }

        int them = 0, loi = 0;
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        var last = await _db.TacGias.OrderByDescending(t => t.MaTacGia).Select(t => t.MaTacGia).FirstOrDefaultAsync();
        int nextNum = 1;
        if (!string.IsNullOrEmpty(last) && last.StartsWith("TG") && int.TryParse(last[2..], out int ln)) nextNum = ln + 1;

        // Headers: TenTacGia | QuocTich | ButDanh | NamSinh | NamMat | TieuSu
        for (int row = 2; row <= (ws.LastRowUsed()?.RowNumber() ?? 1); row++)
        {
            var tenTG = ws.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrEmpty(tenTG)) continue;
            try
            {
                string ma = "TG" + nextNum.ToString("D3");
                while (await _db.TacGias.AnyAsync(t => t.MaTacGia == ma)) { nextNum++; ma = "TG" + nextNum.ToString("D3"); }
                int? namSinh = int.TryParse(ws.Cell(row, 4).GetString().Trim(), out int ns) ? ns : null;
                int? namMat = int.TryParse(ws.Cell(row, 5).GetString().Trim(), out int nm) ? nm : null;
                _db.TacGias.Add(new TacGia
                {
                    MaTacGia = ma,
                    TenTacGia = tenTG,
                    QuocTich = ws.Cell(row, 2).GetString().Trim(),
                    ButDanh = ws.Cell(row, 3).GetString().Trim(),
                    NamSinh = namSinh,
                    NamMat = namMat,
                    TieuSu = ws.Cell(row, 6).GetString().Trim()
                });
                nextNum++; them++;
            }
            catch { loi++; }
        }
        await _db.SaveChangesAsync();
        TempData["success"] = $"Import {them} tác giả thành công. Lỗi: {loi}.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult DownloadTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("TacGia");
        string[] h = { "Tên tác giả (*)", "Quốc tịch", "Bút danh", "Năm sinh", "Năm mất", "Tiểu sử" };
        for (int i = 0; i < h.Length; i++) { ws.Cell(1, i + 1).Value = h[i]; ws.Cell(1, i + 1).Style.Font.Bold = true; ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen; }
        ws.Cell(2, 1).Value = "Nguyễn Văn A"; ws.Cell(2, 2).Value = "Việt Nam"; ws.Cell(2, 3).Value = ""; ws.Cell(2, 4).Value = 1980; ws.Cell(2, 5).Value = ""; ws.Cell(2, 6).Value = "Tác giả nổi tiếng";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_TacGia.xlsx");
    }
}