using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.Services;
using SmartLib.Web.ViewModels;
using System.Security.Claims;

namespace SmartLib.Web.Controllers;

public class AuthController : Controller
{
    private readonly SmartLibDbContext _context;
    private readonly EmailService _emailService;

    public AuthController(SmartLibDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // ── LOGIN ──────────────────────────────────────────────
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _context.NhanViens
            .Include(n => n.ChucVu)
            .FirstOrDefaultAsync(x => x.Email == model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhau))
        {
            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View(model);
        }

        if (!user.TrangThai)
        {
            ViewBag.Error = "Tài khoản đã bị khóa. Vui lòng liên hệ thư viện.";
            return View(model);
        }

        // Kiểm tra tài khoản STU đã xác minh email chưa
        if (user.MaChucVu == "STU" && user.EmailVerified == false)
        {
            ViewBag.Error = "Email chưa được xác minh. Vui lòng kiểm tra hộp thư và xác minh email trước khi đăng nhập.";
            ViewBag.ShowResendVerify = true;
            ViewBag.PendingEmail = user.Email;
            return View(model);
        }

        // Kiểm tra tài khoản STU đã được duyệt chưa
        if (user.MaChucVu == "STU" && !string.IsNullOrEmpty(user.MaDocGia))
        {
            var dg = await _context.DocGias.FindAsync(user.MaDocGia);
            if (dg != null && !dg.DaDuyet)
            {
                ViewBag.Error = "Tài khoản của bạn đang chờ thủ thư xác nhận.";
                return View(model);
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,  user.HoTen),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role,  user.MaChucVu ?? ""),
            new("MaNV",           user.MaNV),
            new("MaDocGia",       user.MaDocGia ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // Ghi nhật ký đăng nhập
        _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
        {
            MaNV = user.MaNV,
            HanhDong = "Đăng nhập",
            MoTa = $"{user.HoTen} ({user.MaChucVu}) đăng nhập vào hệ thống lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            ThoiGian = DateTime.Now
        });
        await _context.SaveChangesAsync();

        return user.MaChucVu is "ADMIN" or "LIB"
            ? RedirectToAction("Index", "Staff")
            : RedirectToAction("Index", "Student");
    }

    // ── REGISTER ───────────────────────────────────────────
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string HoTen, string Email, string? SoDienThoai,
        string? Lop, string? Khoa,
        string MatKhau, string XacNhanMatKhau,
        string MaTheTV)
    {
        if (string.IsNullOrWhiteSpace(HoTen) || string.IsNullOrWhiteSpace(Email)
            || string.IsNullOrWhiteSpace(MatKhau) || string.IsNullOrWhiteSpace(MaTheTV))
        {
            ViewBag.Error = "Vui lòng điền đầy đủ thông tin bắt buộc (bao gồm mã thẻ thư viện).";
            return View();
        }

        if (MatKhau != XacNhanMatKhau) { ViewBag.Error = "Mật khẩu xác nhận không khớp."; return View(); }
        if (MatKhau.Length < 6) { ViewBag.Error = "Mật khẩu phải ít nhất 6 ký tự."; return View(); }

        // ── Kiểm tra mã thẻ thư viện phải tồn tại trên hệ thống ──
        var the = await _context.TheThiViens
            .Include(t => t.DocGia)
            .FirstOrDefaultAsync(t => t.MaThe == MaTheTV);

        if (the == null)
        {
            ViewBag.Error = "Mã thẻ thư viện không tồn tại trên hệ thống. Vui lòng liên hệ thủ thư để được cấp thẻ.";
            return View();
        }

        if (!the.TrangThai)
        {
            ViewBag.Error = "Thẻ thư viện này đã bị vô hiệu hóa. Vui lòng liên hệ thủ thư.";
            return View();
        }

        if (the.MaDocGia != null)
        {
            ViewBag.Error = "Mã thẻ thư viện này đã được liên kết với một tài khoản khác.";
            return View();
        }

        if (await _context.NhanViens.AnyAsync(n => n.Email == Email))
        {
            ViewBag.Error = "Email này đã được đăng ký.";
            return View();
        }

        if (!await _context.ChucVus.AnyAsync(c => c.MaChucVu == "STU"))
            _context.ChucVus.Add(new ChucVu { MaChucVu = "STU", TenChucVu = "Sinh Viên" });

        // Tạo DocGia từ thông tin thẻ
        var lastDG = await _context.DocGias
            .OrderByDescending(d => d.MaDocGia)
            .Select(d => d.MaDocGia)
            .FirstOrDefaultAsync();
        string newMaDG = "DG001";
        if (!string.IsNullOrEmpty(lastDG) && lastDG.StartsWith("DG") && int.TryParse(lastDG[2..], out int dgNum))
            newMaDG = "DG" + (dgNum + 1).ToString("D3");

        var docGia = new DocGia
        {
            MaDocGia = newMaDG,
            HoTen = HoTen,
            Lop = Lop,
            Khoa = Khoa,
            Email = Email,
            SoDienThoai = SoDienThoai,
            NgayTaoThe = DateTime.Now,
            TrangThaiThe = true,
            MaTheTV = MaTheTV,
            DaDuyet = false,
            AnhDaiDien = the.AnhThe
        };

        the.MaDocGia = newMaDG;
        _context.DocGias.Add(docGia);

        // Tạo NhanVien (STU)
        var lastNV = await _context.NhanViens
            .OrderByDescending(n => n.MaNV)
            .Select(n => n.MaNV)
            .FirstOrDefaultAsync();
        string newMaNV = "NV001";
        if (!string.IsNullOrEmpty(lastNV) && lastNV.StartsWith("NV") && int.TryParse(lastNV[2..], out int nvNum))
            newMaNV = "NV" + (nvNum + 1).ToString("D3");

        // Tạo OTP xác minh email
        string otp = new Random().Next(100000, 999999).ToString();

        _context.NhanViens.Add(new NhanVien
        {
            MaNV = newMaNV,
            HoTen = HoTen,
            Email = Email,
            SoDienThoai = SoDienThoai,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(MatKhau),
            MaChucVu = "STU",
            TrangThai = true,
            MaDocGia = newMaDG,
            NgayTao = DateTime.Now,
            OtpCode = otp,
            OtpExpiry = DateTime.Now.AddMinutes(10),
            EmailVerified = false
        });

        await _context.SaveChangesAsync();

        // Gửi OTP xác minh email
        try
        {
            await _emailService.SendOtpAsync(Email, HoTen, otp, "register");
            TempData["VerifyEmail"] = Email;
            TempData["VerifyName"] = HoTen;
            return RedirectToAction(nameof(VerifyEmail));
        }
        catch
        {
            // Nếu gửi email lỗi, vẫn cho đăng ký nhưng thông báo
            ViewBag.Success = "Đăng ký thành công! Tuy nhiên không gửi được email xác minh. Vui lòng liên hệ thủ thư.";
            return View();
        }
    }

    // ── XÁC MINH EMAIL SAU ĐĂNG KÝ ───────────────────────
    public IActionResult VerifyEmail()
    {
        ViewBag.Email = TempData.Peek("VerifyEmail");
        ViewBag.Name = TempData.Peek("VerifyName");
        // Giữ TempData để dùng khi submit
        TempData.Keep("VerifyEmail");
        TempData.Keep("VerifyName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(string email, string OtpInput)
    {
        ViewBag.Email = email;

        var nv = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == email && n.MaChucVu == "STU");

        if (nv == null)
        {
            ViewBag.Error = "Không tìm thấy tài khoản.";
            return View();
        }

        if (nv.OtpCode != OtpInput || nv.OtpExpiry < DateTime.Now)
        {
            ViewBag.Error = "Mã OTP không đúng hoặc đã hết hạn. Vui lòng yêu cầu gửi lại.";
            ViewBag.Email = email;
            return View();
        }

        nv.EmailVerified = true;
        nv.OtpCode = null;
        nv.OtpExpiry = null;
        await _context.SaveChangesAsync();

        TempData["VerifySuccess"] = true;
        return RedirectToAction(nameof(Login));
    }

    // ── GỬI LẠI OTP XÁC MINH ─────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerifyOtp(string email)
    {
        var nv = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == email && n.MaChucVu == "STU");

        if (nv == null)
        {
            TempData["ResendError"] = "Không tìm thấy tài khoản với email này.";
            TempData["VerifyEmail"] = email;
            return RedirectToAction(nameof(VerifyEmail));
        }

        string otp = new Random().Next(100000, 999999).ToString();
        nv.OtpCode = otp;
        nv.OtpExpiry = DateTime.Now.AddMinutes(10);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendOtpAsync(email, nv.HoTen, otp, "register");
            TempData["ResendSuccess"] = $"Đã gửi lại mã OTP về {email}.";
        }
        catch
        {
            TempData["ResendError"] = "Lỗi gửi email. Vui lòng thử lại.";
        }

        TempData["VerifyEmail"] = email;
        TempData["VerifyName"] = nv.HoTen;
        return RedirectToAction(nameof(VerifyEmail));
    }

    // ── LOGOUT ────────────────────────────────────────────
    public async Task<IActionResult> Logout()
    {
        var maNV = User.FindFirst("MaNV")?.Value;
        var hoTen = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(maNV))
        {
            _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                MaNV = maNV,
                HanhDong = "Đăng xuất",
                MoTa = $"{hoTen} đăng xuất khỏi hệ thống lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                ThoiGian = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ── API kiểm tra mã thẻ (AJAX từ trang đăng ký) ───────
    [HttpGet]
    public async Task<IActionResult> CheckCard(string ma)
    {
        if (string.IsNullOrWhiteSpace(ma))
            return Json(new { ok = false, message = "Vui lòng nhập mã thẻ." });

        var the = await _context.TheThiViens
            .Include(t => t.DocGia)
            .FirstOrDefaultAsync(t => t.MaThe == ma.ToUpper().Trim());

        if (the == null)
            return Json(new { ok = false, message = "Mã thẻ không tồn tại trên hệ thống." });
        if (!the.TrangThai)
            return Json(new { ok = false, message = "Thẻ thư viện này đã bị vô hiệu hóa." });
        if (the.MaDocGia != null)
            return Json(new { ok = false, message = "Thẻ này đã được liên kết với tài khoản khác." });

        return Json(new { ok = true, message = $"Thẻ hợp lệ. Hết hạn: {the.NgayHetHan:dd/MM/yyyy}" });
    }
}
