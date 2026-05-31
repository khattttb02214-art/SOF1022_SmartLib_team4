using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    [Authorize(Roles = "ADMIN,ThuThu")]
    public class TheLoaiController : Controller
    {
        private readonly SmartLibDbContext _context;

        public TheLoaiController(SmartLibDbContext context)
        {
            _context = context;
        }

        // 1. GET: Hiển thị danh sách thể loại
        public async Task<IActionResult> Index(string searchString)
        {
            // Khởi tạo query từ bảng TheLoais
            var danhSach = _context.TheLoais.AsQueryable();

            // Nếu có từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                string s = searchString.ToLower();
                // Lọc theo Mã hoặc Tên (không phân biệt hoa thường)
                danhSach = danhSach.Where(t => t.MaTheLoai.ToLower().Contains(s) ||
                                               t.TenTheLoai.ToLower().Contains(s));
            }

            // Thực thi và trả về View
            return View(await danhSach.ToListAsync());
        }

        // 2. GET: Giao diện thêm mới thể loại
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. POST: Xử lý lưu thể loại mới vào SQL Server
        [HttpPost]
        public async Task<IActionResult> Create(TheLoai theLoai)
        {
            if (ModelState.IsValid)
            {
                _context.Add(theLoai);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Lưu xong thì bay về trang danh sách
            }
            return View(theLoai);
        }
    }
}