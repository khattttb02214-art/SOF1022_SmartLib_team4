using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class NhaXuatBanController : Controller
{
    private readonly SmartLibDbContext _db;
    public NhaXuatBanController(SmartLibDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var q = _db.NhaXuatBans.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(n => n.TenNXB.Contains(search));
        ViewBag.Search = search;
        return View(await q.OrderBy(n => n.TenNXB).ToListAsync());
    }

    public async Task<IActionResult> Detail(string id)
    {
        var nxb = await _db.NhaXuatBans
            .Include(n => n.Saches).ThenInclude(s => s.TheLoai)
            .FirstOrDefaultAsync(n => n.MaNXB == id);
        if (nxb == null) return NotFound();
        return View(nxb);
    }

    public IActionResult Create() => View(new NhaXuatBanViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NhaXuatBanViewModel m)
    {
        if (!ModelState.IsValid) return View(m);

        // Auto-generate MaNXB
        if (string.IsNullOrWhiteSpace(m.MaNXB))
        {
            var last = await _db.NhaXuatBans.OrderByDescending(n => n.MaNXB).Select(n => n.MaNXB).FirstOrDefaultAsync();
            string newMa = "NXB01";
            if (!string.IsNullOrEmpty(last) && last.StartsWith("NXB") && int.TryParse(last[3..], out int n))
                newMa = "NXB" + (n + 1).ToString("D2");
            m.MaNXB = newMa;
        }

        if (await _db.NhaXuatBans.AnyAsync(n => n.MaNXB == m.MaNXB))
        { ModelState.AddModelError("MaNXB", "Mã đã tồn tại"); return View(m); }
        _db.NhaXuatBans.Add(new NhaXuatBan { MaNXB = m.MaNXB, TenNXB = m.TenNXB, DiaChi = m.DiaChi, SoDienThoai = m.SoDienThoai, Email = m.Email });
        await _db.SaveChangesAsync();
        TempData["success"] = "Thêm nhà xuất bản thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var n = await _db.NhaXuatBans.FindAsync(id);
        if (n == null) return NotFound();
        return View(new NhaXuatBanViewModel { MaNXB = n.MaNXB, TenNXB = n.TenNXB, DiaChi = n.DiaChi, SoDienThoai = n.SoDienThoai, Email = n.Email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, NhaXuatBanViewModel m)
    {
        if (!ModelState.IsValid) return View(m);
        var n = await _db.NhaXuatBans.FindAsync(id);
        if (n == null) return NotFound();
        n.TenNXB = m.TenNXB; n.DiaChi = m.DiaChi; n.SoDienThoai = m.SoDienThoai; n.Email = m.Email;
        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật nhà xuất bản thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var n = await _db.NhaXuatBans.FindAsync(id);
        if (n == null) return NotFound();
        if (await _db.Saches.AnyAsync(s => s.MaNXB == id))
        { TempData["error"] = "Không thể xóa: NXB này vẫn có sách liên kết"; return RedirectToAction(nameof(Index)); }
        _db.NhaXuatBans.Remove(n);
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã xóa nhà xuất bản";
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

        var last = await _db.NhaXuatBans.OrderByDescending(n => n.MaNXB).Select(n => n.MaNXB).FirstOrDefaultAsync();
        int nextNum = 1;
        if (!string.IsNullOrEmpty(last) && last.StartsWith("NXB") && int.TryParse(last[3..], out int ln)) nextNum = ln + 1;

        // Headers: TenNXB | DiaChi | SoDienThoai | Email
        for (int row = 2; row <= (ws.LastRowUsed()?.RowNumber() ?? 1); row++)
        {
            var ten = ws.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrEmpty(ten)) continue;
            try
            {
                string ma = "NXB" + nextNum.ToString("D2");
                while (await _db.NhaXuatBans.AnyAsync(n => n.MaNXB == ma)) { nextNum++; ma = "NXB" + nextNum.ToString("D2"); }
                _db.NhaXuatBans.Add(new NhaXuatBan
                {
                    MaNXB = ma,
                    TenNXB = ten,
                    DiaChi = ws.Cell(row, 2).GetString().Trim(),
                    SoDienThoai = ws.Cell(row, 3).GetString().Trim(),
                    Email = ws.Cell(row, 4).GetString().Trim()
                });
                nextNum++; them++;
            }
            catch { loi++; }
        }
        await _db.SaveChangesAsync();
        TempData["success"] = $"Import {them} nhà xuất bản thành công. Lỗi: {loi}.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult DownloadTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("NhaXuatBan");
        string[] h = { "Tên NXB (*)", "Địa chỉ", "Số điện thoại", "Email" };
        for (int i = 0; i < h.Length; i++) { ws.Cell(1, i + 1).Value = h[i]; ws.Cell(1, i + 1).Style.Font.Bold = true; ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow; }
        ws.Cell(2, 1).Value = "NXB Giáo Dục"; ws.Cell(2, 2).Value = "Hà Nội"; ws.Cell(2, 3).Value = "0241234567"; ws.Cell(2, 4).Value = "contact@nxbgd.vn";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_NhaXuatBan.xlsx");
    }
}