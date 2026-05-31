using Assignment.Models;
using Assignment.Models.Entities; // Đảm bảo đúng namespace này nhé
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    [Authorize(Roles = "ADMIN,ThuThu")]
    public class TacGiaController : Controller
    {
        private readonly SmartLibDbContext _context;

        public TacGiaController(SmartLibDbContext context)
        {
            _context = context;
        }

        // GET: TacGia
        public async Task<IActionResult> Index(string searchString)
        {
            // Lưu từ khóa tìm kiếm vào ViewBag để nó hiện lại trên ô input sau khi tìm
            ViewBag.CurrentFilter = searchString;

            var tacGias = from t in _context.TacGia select t;

            // Nếu người dùng nhập gì đó vào ô tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm theo tên tác giả
                tacGias = tacGias.Where(t => t.TenTacGia.Contains(searchString));
            }

            return View(await tacGias.ToListAsync());
        }

        // GET: TacGia/Create
        public IActionResult Create() => View();

        // POST: TacGia/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TacGium tacGia)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tacGia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tacGia);
        }

        // GET: TacGia/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var tacGia = await _context.TacGia.FindAsync(id);
            if (tacGia == null) return NotFound();
            return View(tacGia);
        }

        // POST: TacGia/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, TacGium tacGia)
        {
            if (id != tacGia.MaTacGia) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(tacGia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tacGia);
        }

        // POST: TacGia/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var tacGia = await _context.TacGia
                                        .Include(t => t.MaSaches) // "Include" để lấy danh sách sách đi kèm
                                        .FirstOrDefaultAsync(t => t.MaTacGia == id);

            if (tacGia == null) return NotFound();

            // KIỂM TRA: Nếu có sách thì không cho xóa
            if (tacGia.MaSaches.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa tác giả này vì đang có sách liên quan trong hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            _context.TacGia.Remove(tacGia);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa tác giả thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}