using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment.Controllers
{
    [Authorize(Roles = "ADMIN,ThuThu")]
    public class NhaXuatBanController : Controller
    {
        private readonly SmartLibDbContext _context;

        public NhaXuatBanController(SmartLibDbContext context)
        {
            _context = context;
        }

        // GET: NhaXuatBan
        public async Task<IActionResult> Index(string searchString)
        {
            // Lấy IQueryable để có thể lọc
            var nxbList = _context.NhaXuatBans.AsQueryable();

            // Nếu có từ khóa thì lọc theo Tên NXB
            if (!string.IsNullOrEmpty(searchString))
            {
                nxbList = nxbList.Where(n => n.TenNxb.Contains(searchString));
            }

            // Lưu từ khóa vào ViewBag để giữ giá trị ô tìm kiếm
            ViewBag.CurrentFilter = searchString;

            return View(await nxbList.ToListAsync());
        }

        // GET: NhaXuatBan/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaXuatBan = await _context.NhaXuatBans
                .FirstOrDefaultAsync(m => m.MaNxb == id);
            if (nhaXuatBan == null)
            {
                return NotFound();
            }

            return View(nhaXuatBan);
        }

        // GET: NhaXuatBan/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: NhaXuatBan/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNxb,TenNxb,DiaChi,SoDienThoai,Email")] NhaXuatBan nhaXuatBan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(nhaXuatBan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(nhaXuatBan);
        }

        // GET: NhaXuatBan/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaXuatBan = await _context.NhaXuatBans.FindAsync(id);
            if (nhaXuatBan == null)
            {
                return NotFound();
            }
            return View(nhaXuatBan);
        }

        // POST: NhaXuatBan/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaNxb,TenNxb,DiaChi,SoDienThoai,Email")] NhaXuatBan nhaXuatBan)
        {
            if (id != nhaXuatBan.MaNxb)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nhaXuatBan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhaXuatBanExists(nhaXuatBan.MaNxb))
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
            return View(nhaXuatBan);
        }

        // GET: NhaXuatBan/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaXuatBan = await _context.NhaXuatBans
                .FirstOrDefaultAsync(m => m.MaNxb == id);
            if (nhaXuatBan == null)
            {
                return NotFound();
            }

            return View(nhaXuatBan);
        }

        // POST: NhaXuatBan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // Thêm logic chặn xóa vào đây
            bool dangCoSach = await _context.Saches.AnyAsync(s => s.MaNxb == id);

            if (dangCoSach)
            {
                TempData["ErrorMessage"] = "Không thể xóa Nhà xuất bản này vì vẫn còn sách đang liên kết!";
                return RedirectToAction(nameof(Index));
            }

            var nhaXuatBan = await _context.NhaXuatBans.FindAsync(id);
            if (nhaXuatBan != null)
            {
                _context.NhaXuatBans.Remove(nhaXuatBan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NhaXuatBanExists(string id)
        {
            return _context.NhaXuatBans.Any(e => e.MaNxb == id);
        }
    }
}
