using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    public class WishlistController : Controller
    {
        private readonly SmartLibDbContext _context;
        public WishlistController(SmartLibDbContext context) => _context = context;

        // Xem danh sách wishlist của mình
        [Authorize(Roles = "DocGia,ADMIN,ThuThu")]
        public async Task<IActionResult> Index()
        {
            var maDocGia = User.FindFirst("MaDocGia")?.Value;

            if (User.IsInRole("DocGia"))
            {
                // DocGia chỉ thấy wishlist của mình
                var wishlist = await _context.Wishlists
                    .Include(w => w.MaSachNavigation)
                        .ThenInclude(s => s.MaTacGia)
                    .Include(w => w.MaDocGiaNavigation)
                    .Where(w => w.MaDocGia == maDocGia)
                    .ToListAsync();
                return View(wishlist);
            }
            else
            {
                // ADMIN/ThuThu thấy tất cả
                var wishlist = await _context.Wishlists
                    .Include(w => w.MaSachNavigation)
                        .ThenInclude(s => s.MaTacGia)
                    .Include(w => w.MaDocGiaNavigation)
                    .ToListAsync();
                return View(wishlist);
            }
        }

        // Thêm vào wishlist
        [HttpPost]
        [Authorize(Roles = "DocGia")]
        public async Task<IActionResult> ThemVaoWishlist(string maSach)
        {
            var maDocGia = User.FindFirst("MaDocGia")?.Value;
            if (maDocGia == null) return RedirectToAction("Login", "Account");

            // Kiểm tra đã có trong wishlist chưa
            bool daCoTrongWishlist = await _context.Wishlists
                .AnyAsync(w => w.MaDocGia == maDocGia && w.MaSach == maSach);

            if (!daCoTrongWishlist)
            {
                _context.Wishlists.Add(new Wishlist
                {
                    MaDocGia = maDocGia,
                    MaSach = maSach,
                    NgayThem = DateOnly.FromDateTime(DateTime.Now)
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Library", "Home");
        }

        // Xóa khỏi wishlist
        [HttpPost]
        [Authorize(Roles = "DocGia")]
        public async Task<IActionResult> Xoa(string maSach)
        {
            var maDocGia = User.FindFirst("MaDocGia")?.Value;
            if (maDocGia == null) return RedirectToAction("Login", "Account");

            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.MaDocGia == maDocGia && w.MaSach == maSach);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}