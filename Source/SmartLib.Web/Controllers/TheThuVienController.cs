using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class TheThuVienController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly IWebHostEnvironment _env;

    public TheThuVienController(SmartLibDbContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    // ── Sinh mã thẻ tự động ──────────────────────────────
    private async Task<string> SinhMaThe()
    {
        var last = await _db.TheThiViens
            .OrderByDescending(t => t.Id)
            .Select(t => t.MaThe)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(last))
            return "THE00001";

        // Extract number part after prefix
        if (last.StartsWith("THE") && int.TryParse(last[3..], out int num))
            return "THE" + (num + 1).ToString("D5");

        return "THE" + (await _db.TheThiViens.CountAsync() + 1).ToString("D5");
    }

    public async Task<IActionResult> Index(string? search)
    {
        var q = _db.TheThiViens.Include(t => t.DocGia).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(t => t.MaThe.Contains(search)
                || (t.DocGia != null && t.DocGia.HoTen.Contains(search)));
        ViewBag.Search = search;
        return View(await q.OrderByDescending(t => t.NgayCap).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.DocGia = new SelectList(
            await _db.DocGias.OrderBy(d => d.HoTen).ToListAsync(),
            "MaDocGia", "HoTen");
        ViewBag.MaTheTuSinh = await SinhMaThe();
        return View(new TheThuVienViewModel { NgayHetHan = DateTime.Now.AddYears(4) });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TheThuVienViewModel model)
    {
        // Auto-generate MaThe if not provided
        if (string.IsNullOrWhiteSpace(model.MaThe))
            model.MaThe = await SinhMaThe();

        // Check duplicate
        if (await _db.TheThiViens.AnyAsync(t => t.MaThe == model.MaThe))
        {
            ModelState.AddModelError("MaThe", "Mã thẻ đã tồn tại");
            await LoadDG(); return View(model);
        }

        // Check DocGia already has a valid card
        if (!string.IsNullOrEmpty(model.MaDocGia))
        {
            var existingCard = await _db.TheThiViens
                .FirstOrDefaultAsync(t => t.MaDocGia == model.MaDocGia && t.TrangThai);
            if (existingCard != null)
            {
                ModelState.AddModelError("MaDocGia", $"Sinh viên này đã có thẻ thư viện còn hiệu lực ({existingCard.MaThe})");
                await LoadDG(); return View(model);
            }
        }

        string? anhThe = null;
        if (model.AnhTheFile != null)
        {
            anhThe = Guid.NewGuid() + Path.GetExtension(model.AnhTheFile.FileName);
            var folder = Path.Combine(_env.WebRootPath, "uploads/cards");
            Directory.CreateDirectory(folder);
            await using var fs = new FileStream(Path.Combine(folder, anhThe), FileMode.Create);
            await model.AnhTheFile.CopyToAsync(fs);

            if (!string.IsNullOrEmpty(model.MaDocGia))
            {
                var dg = await _db.DocGias.FindAsync(model.MaDocGia);
                if (dg != null && string.IsNullOrEmpty(dg.AnhDaiDien))
                {
                    var srcPath = Path.Combine(_env.WebRootPath, "uploads/cards", anhThe);
                    var dstName = Guid.NewGuid() + Path.GetExtension(model.AnhTheFile.FileName);
                    var dstPath = Path.Combine(_env.WebRootPath, "uploads/readers", dstName);
                    Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads/readers"));
                    System.IO.File.Copy(srcPath, dstPath);
                    dg.AnhDaiDien = dstName;
                }
            }
        }

        _db.TheThiViens.Add(new TheThuVien {
            MaThe = model.MaThe, MaDocGia = model.MaDocGia,
            NgayHetHan = model.NgayHetHan, TrangThai = model.TrangThai,
            AnhThe = anhThe, NgayCap = DateTime.Now
        });
        await _db.SaveChangesAsync();
        TempData["success"] = $"Tạo thẻ thư viện {model.MaThe} thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var t = await _db.TheThiViens.FindAsync(id);
        if (t == null) return NotFound();
        await LoadDG();
        return View(new TheThuVienViewModel {
            Id = t.Id, MaThe = t.MaThe, MaDocGia = t.MaDocGia!,
            NgayHetHan = t.NgayHetHan, TrangThai = t.TrangThai
        });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TheThuVienViewModel model)
    {
        var t = await _db.TheThiViens.FindAsync(id);
        if (t == null) return NotFound();
        t.NgayHetHan = model.NgayHetHan; t.TrangThai = model.TrangThai;

        if (model.AnhTheFile != null)
        {
            var anhThe = Guid.NewGuid() + Path.GetExtension(model.AnhTheFile.FileName);
            var folder = Path.Combine(_env.WebRootPath, "uploads/cards");
            Directory.CreateDirectory(folder);
            await using var fs = new FileStream(Path.Combine(folder, anhThe), FileMode.Create);
            await model.AnhTheFile.CopyToAsync(fs);
            t.AnhThe = anhThe;
        }
        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật thẻ thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.TheThiViens.FindAsync(id);
        if (t == null) return NotFound();
        _db.TheThiViens.Remove(t);
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã xóa thẻ";
        return RedirectToAction(nameof(Index));
    }

    // ── API: get next card code (for AJAX) ───────────────
    [HttpGet]
    public async Task<IActionResult> GetNextCode()
        => Json(new { code = await SinhMaThe() });

    private async Task LoadDG() =>
        ViewBag.DocGia = new SelectList(
            await _db.DocGias.OrderBy(d => d.HoTen).ToListAsync(),
            "MaDocGia", "HoTen");
}
