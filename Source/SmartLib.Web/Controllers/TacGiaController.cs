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
    public TacGiaController(SmartLibDbContext db) => _db = db;

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

        _db.TacGias.Add(new TacGia { MaTacGia = model.MaTacGia, TenTacGia = model.TenTacGia, TieuSu = model.TieuSu, QuocTich = model.QuocTich });
        await _db.SaveChangesAsync();
        TempData["success"] = "Thêm tác giả thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var t = await _db.TacGias.FindAsync(id);
        if (t == null) return NotFound();
        return View(new TacGiaViewModel { MaTacGia = t.MaTacGia, TenTacGia = t.TenTacGia, TieuSu = t.TieuSu, QuocTich = t.QuocTich });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TacGiaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var t = await _db.TacGias.FindAsync(id);
        if (t == null) return NotFound();
        t.TenTacGia = model.TenTacGia; t.TieuSu = model.TieuSu; t.QuocTich = model.QuocTich;
        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật tác giả thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var t = await _db.TacGias.FindAsync(id);
        if (t == null) return NotFound();
        _db.TacGias.Remove(t);
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã xóa tác giả";
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

        // Headers: TenTacGia | QuocTich | TieuSu
        for (int row = 2; row <= (ws.LastRowUsed()?.RowNumber() ?? 1); row++)
        {
            var tenTG = ws.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrEmpty(tenTG)) continue;
            try
            {
                string ma = "TG" + nextNum.ToString("D3");
                while (await _db.TacGias.AnyAsync(t => t.MaTacGia == ma)) { nextNum++; ma = "TG" + nextNum.ToString("D3"); }
                _db.TacGias.Add(new TacGia
                {
                    MaTacGia = ma,
                    TenTacGia = tenTG,
                    QuocTich = ws.Cell(row, 2).GetString().Trim(),
                    TieuSu = ws.Cell(row, 3).GetString().Trim()
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
        string[] h = { "Tên tác giả (*)", "Quốc tịch", "Tiểu sử" };
        for (int i = 0; i < h.Length; i++) { ws.Cell(1, i + 1).Value = h[i]; ws.Cell(1, i + 1).Style.Font.Bold = true; ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen; }
        ws.Cell(2, 1).Value = "Nguyễn Văn A"; ws.Cell(2, 2).Value = "Việt Nam"; ws.Cell(2, 3).Value = "Tác giả nổi tiếng";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_TacGia.xlsx");
    }
}