using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Assignment.Controllers
{
    public class HomeController : Controller
    {
        private readonly SmartLibDbContext _context;
        public HomeController(SmartLibDbContext context) { _context = context; }

        // Trang chủ cho mọi người (Khách vãng lai + Admin)
        public async Task<IActionResult> Index()
        {
            // Lấy 4 cuốn sách mới nhất làm nổi bật
            var moiNhat = await _context.Saches
                .OrderByDescending(s => s.NgayTao)
                .Take(4)
                .ToListAsync();
            return View(moiNhat);
        }

        // Trang liệt kê sách (Dành cho khách xem)
        public async Task<IActionResult> Library()
        {
            var listSach = await _context.Saches
                .Include(s => s.MaTacGia)
                .Include(s => s.CuonSaches) // THÊM DÒNG NÀY
                .Where(s => s.TrangThai == true)
                .ToListAsync();

            // Tính lại số lượng thực tế
            foreach (var sach in listSach)
            {
                sach.SoLuongKho = sach.CuonSaches.Count();
                sach.SoLuongKhaDung = sach.CuonSaches.Count(c => c.TrangThai == "Có sẵn");
            }

            return View(listSach);
        }

        // Action Mượn sách (Cần bảo mật)
        public IActionResult RequestBook(string maSach)
        {
            // 1. Kiểm tra đăng nhập tại chỗ
            if (!User.Identity.IsAuthenticated)
            {
                // Lưu trang hiện tại để quay lại
                var returnUrl = Url.Action("Library", "Home");
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }

            // 2. Code xử lý mượn sách...
            return View();
        }
        [HttpGet]
        [Authorize(Roles = "DocGia")]
        public async Task<IActionResult> DangKyMuon(string maSach)
        {
            var sach = await _context.Saches
                .Include(s => s.MaTacGia)
                .FirstOrDefaultAsync(s => s.MaSach == maSach);

            if (sach == null) return NotFound();
            return View(sach);
        }

        [HttpPost]
        [Authorize(Roles = "DocGia")]
        public async Task<IActionResult> DangKyMuon(string maSach, DateOnly ngayHenTra)
        {
            var maDocGia = User.FindFirst("MaDocGia")?.Value;
            if (maDocGia == null) return RedirectToAction("Login", "Account");

            // Tạo mã phiếu tự động
            var soPhieu = await _context.MuonTras.CountAsync() + 1;
            var maPhieu = $"PM{soPhieu:D3}";

            var phieu = new MuonTra
            {
                MaPhieu = maPhieu,
                MaDocGia = maDocGia,
                NgayMuon = DateOnly.FromDateTime(DateTime.Now),
                NgayHenTra = ngayHenTra,
                TrangThai = "Chờ xác nhận",
                GhiChu = maSach  // Chỉ lưu MaSach thôi, không có chữ khác
            };

            _context.MuonTras.Add(phieu);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Yêu cầu mượn sách đã được gửi! Vui lòng đến thư viện để nhận sách.";
            return RedirectToAction("Library");
        }
    }
}
