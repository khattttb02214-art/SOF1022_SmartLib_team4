
using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "ADMIN,ThuThu")]
public class KeSachController : Controller
{
    private readonly SmartLibDbContext _context;

    public KeSachController(SmartLibDbContext context)
    {
        _context = context;
    }

    // GET: KESACHS
    public async Task<IActionResult> Index(string searchString)
    {
        // 1. Tạo truy vấn ban đầu (chưa lấy dữ liệu)
        var keSaches = _context.KeSaches.Include(k => k.Saches).AsQueryable();

        // 2. Nếu có từ khóa, lọc theo tên kệ
        if (!string.IsNullOrEmpty(searchString))
        {
            keSaches = keSaches.Where(k => k.TenKe.Contains(searchString));
        }

        // 3. Đưa từ khóa vào ViewBag để giữ giá trị trong ô tìm kiếm
        ViewBag.CurrentFilter = searchString;

        // 4. Trả về kết quả
        return View(await keSaches.ToListAsync());
    }

    // GET: KESACHS/Details/5
    public async Task<IActionResult> Details(string? make)
    {
        if (make == null)
        {
            return NotFound();
        }

        var kesach = await _context.KeSaches
            .FirstOrDefaultAsync(m => m.MaKe == make);
        if (kesach == null)
        {
            return NotFound();
        }

        return View(kesach);
    }

    // GET: KESACHS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: KESACHS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaKe,TenKe,ViTri,Saches")] KeSach kesach)
    {
        if (ModelState.IsValid)
        {
            _context.Add(kesach);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(kesach);
    }

    // GET: KESACHS/Edit/5
    public async Task<IActionResult> Edit(string? make)
    {
        if (make == null)
        {
            return NotFound();
        }

        var kesach = await _context.KeSaches.FindAsync(make);
        if (kesach == null)
        {
            return NotFound();
        }
        return View(kesach);
    }

    // POST: KESACHS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? make, [Bind("MaKe,TenKe,ViTri,Saches")] KeSach kesach)
    {
        if (make != kesach.MaKe)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(kesach);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KeSachExists(kesach.MaKe))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(kesach);
    }

    // GET: KESACHS/Delete/5
    public async Task<IActionResult> Delete(string? make)
    {
        if (make == null)
        {
            return NotFound();
        }

        var kesach = await _context.KeSaches
            .FirstOrDefaultAsync(m => m.MaKe == make);
        if (kesach == null)
        {
            return NotFound();
        }

        return View(kesach);
    }

    // POST: KESACHS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        // Kiểm tra xem Kệ này có đang chứa sách nào không
        bool dangCoSach = await _context.Saches.AnyAsync(s => s.MaKe == id);

        if (dangCoSach)
        {
            TempData["ErrorMessage"] = "Không thể xóa kệ này vì vẫn còn sách đang đặt trên kệ!";
            return RedirectToAction(nameof(Index));
        }

        var keSach = await _context.KeSaches.FindAsync(id);
        if (keSach != null)
        {
            _context.KeSaches.Remove(keSach);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool KeSachExists(string? make)
    {
        return _context.KeSaches.Any(e => e.MaKe == make);
    }
}
