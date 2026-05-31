using Assignment.Models;
using Assignment.Models.Entities;
using Assignment.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Assignment.Controllers
{
    public class AccountController : Controller
    {
        private readonly SmartLibDbContext _context;

        public AccountController(SmartLibDbContext context)
        {
            _context = context;
        }
        public IActionResult AccessDenied()
        {
            return View(); // Tạo một trang báo: "Bạn không có quyền truy cập!"
        }
        [HttpGet] // Phải có để hiển thị form
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == model.Email && nv.MatKhau == model.MatKhau);

            var docGia = (nhanVien == null) ? await _context.DocGia
                .FirstOrDefaultAsync(dg => dg.Email == model.Email && dg.MatKhau == model.MatKhau) : null;

            if (nhanVien != null || docGia != null)
            {
                // Logic lấy đúng chức vụ từ Database
                string role = "DocGia";
                if (nhanVien != null)
                {
                    // Lấy chính xác cột MaChucVu từ bảng NhanVien
                    role = nhanVien.MaChucVu ?? "ThuThu";
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, nhanVien != null ? nhanVien.HoTen : docGia.HoTen),
                    new Claim(ClaimTypes.Role, role),
                    // THÊM DÒNG NÀY để lưu MaDocGia hoặc MaNV vào claim
                    new Claim("MaDocGia", nhanVien != null ? nhanVien.MaNv : docGia.MaDocGia)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}