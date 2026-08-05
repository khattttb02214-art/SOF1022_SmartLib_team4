using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.Services;
using SmartLib.Web.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;

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
        if (TempData["LoginError"] != null)
            ViewBag.Error = TempData["LoginError"];
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        if (user.MaChucVu == "STU" && user.EmailVerified == false)
        {
            ViewBag.Error = "Email chưa được xác minh. Vui lòng kiểm tra hộp thư và xác minh email trước khi đăng nhập.";
            ViewBag.ShowResendVerify = true;
            ViewBag.PendingEmail = user.Email;
            return View(model);
        }

        if (user.MaChucVu == "STU" && !string.IsNullOrEmpty(user.MaDocGia))
        {
            var dg = await _context.DocGias.FindAsync(user.MaDocGia);
            if (dg != null && !dg.DaDuyet)
            {
                ViewBag.Error = "Tài khoản của bạn đang chờ thủ thư xác nhận.";
                return View(model);
            }
        }

        await SignInUserAsync(user);
        return user.MaChucVu is "ADMIN" or "LIB"
            ? RedirectToAction("Index", "Staff")
            : RedirectToAction("Index", "Student");
    }

    // ── QUÊN MẬT KHẨU ────────────────────────────────────────────────────
    // Bước 1: nhập email → gửi OTP. Áp dụng cho MỌI tài khoản (ADMIN/LIB/STU,
    // kể cả tài khoản tạo qua Google) miễn có email hợp lệ trong hệ thống.
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        // Không tiết lộ email có tồn tại hay không (tránh dò quét email) —
        // luôn hiện cùng 1 thông báo chung chung dù tìm thấy tài khoản hay không.
        const string thongBaoChung = "Nếu email này tồn tại trong hệ thống, một mã xác nhận đã được gửi tới hộp thư của bạn.";

        if (string.IsNullOrWhiteSpace(email))
        {
            ViewBag.Error = "Vui lòng nhập email";
            return View();
        }

        var user = await _context.NhanViens.FirstOrDefaultAsync(x => x.Email == email);
        if (user != null)
        {
            string otp = GenerateOtp();
            user.OtpResetMatKhau = otp;
            user.OtpResetMatKhauHetHan = DateTime.Now.AddMinutes(10);
            await _context.SaveChangesAsync();
            await _emailService.SendOtpAsync(user.Email!, user.HoTen, otp, "reset_password");
        }

        TempData["success"] = thongBaoChung;
        return RedirectToAction(nameof(ResetPassword), new { email });
    }

    // Bước 2: nhập mã OTP + mật khẩu mới. Xác thực OTP và cập nhật mật khẩu cùng lúc.
    public IActionResult ResetPassword(string email) => View(model: email);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string email, string otp, string matKhauMoi, string xacNhanMatKhau)
    {
        if (string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(matKhauMoi))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ mã xác nhận và mật khẩu mới";
            return View(model: email);
        }
        if (matKhauMoi.Length < 6)
        {
            ViewBag.Error = "Mật khẩu phải ít nhất 6 ký tự";
            return View(model: email);
        }
        if (matKhauMoi != xacNhanMatKhau)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp";
            return View(model: email);
        }

        var user = await _context.NhanViens.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null || user.OtpResetMatKhau != otp || user.OtpResetMatKhauHetHan == null || user.OtpResetMatKhauHetHan < DateTime.Now)
        {
            ViewBag.Error = "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng yêu cầu gửi lại mã.";
            return View(model: email);
        }

        user.MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);
        user.MatKhauTuDat = true;
        user.OtpResetMatKhau = null;
        user.OtpResetMatKhauHetHan = null;
        await _context.SaveChangesAsync();

        TempData["success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
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

        var the = await _context.TheThiViens
            .Include(t => t.DocGia)
            .FirstOrDefaultAsync(t => t.MaThe == MaTheTV);

        if (the == null) { ViewBag.Error = "Mã thẻ thư viện không tồn tại trên hệ thống. Vui lòng liên hệ thủ thư để được cấp thẻ."; return View(); }
        if (!the.TrangThai) { ViewBag.Error = "Thẻ thư viện này đã bị vô hiệu hóa. Vui lòng liên hệ thủ thư."; return View(); }
        if (the.MaDocGia != null) { ViewBag.Error = "Mã thẻ thư viện này đã được liên kết với một tài khoản khác."; return View(); }
        if (await _context.NhanViens.AnyAsync(n => n.Email == Email)) { ViewBag.Error = "Email này đã được đăng ký."; return View(); }

        if (!await _context.ChucVus.AnyAsync(c => c.MaChucVu == "STU"))
            _context.ChucVus.Add(new ChucVu { MaChucVu = "STU", TenChucVu = "Sinh Viên" });

        var newMaDG = await GenerateMaDGAsync();
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

        var newMaNV = await GenerateMaNVAsync();
        string otp = GenerateOtp();

        _context.NhanViens.Add(new NhanVien
        {
            MaNV = newMaNV,
            HoTen = HoTen,
            Email = Email,
            SoDienThoai = SoDienThoai,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(MatKhau),
            MatKhauTuDat = true, // đăng ký thường → mật khẩu là do chính họ đặt
            MaChucVu = "STU",
            TrangThai = true,
            MaDocGia = newMaDG,
            NgayTao = DateTime.Now,
            OtpCode = otp,
            OtpExpiry = DateTime.Now.AddMinutes(10),
            EmailVerified = false
        });

        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendOtpAsync(Email, HoTen, otp, "register");
            TempData["VerifyEmail"] = Email;
            TempData["VerifyName"] = HoTen;
            return RedirectToAction(nameof(VerifyEmail));
        }
        catch
        {
            ViewBag.Success = "Đăng ký thành công! Tuy nhiên không gửi được email xác minh. Vui lòng liên hệ thủ thư.";
            return View();
        }
    }

    // ── XÁC MINH EMAIL SAU ĐĂNG KÝ ───────────────────────
    public IActionResult VerifyEmail()
    {
        ViewBag.Email = TempData.Peek("VerifyEmail");
        ViewBag.Name = TempData.Peek("VerifyName");
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

        if (nv == null) { ViewBag.Error = "Không tìm thấy tài khoản."; return View(); }

        if (nv.OtpCode != OtpInput || nv.OtpExpiry < DateTime.Now)
        {
            ViewBag.Error = "Mã OTP không đúng hoặc đã hết hạn. Vui lòng yêu cầu gửi lại.";
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

        string otp = GenerateOtp();
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

    // ╔══════════════════════════════════════════════════════╗
    // ║              ĐĂNG NHẬP / ĐĂNG KÝ GOOGLE             ║
    // ╚══════════════════════════════════════════════════════╝

    // Bước 1: Bấm nút Google → chuyển sang trang Google
    // source = "login" | "register"  để biết user đến từ trang nào
    public IActionResult GoogleLogin(string source = "login")
    {
        TempData["GoogleSource"] = source;
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth");
        var props = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    // Bước 2: Google trả kết quả về đây
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            TempData["LoginError"] = "Đăng nhập Google không thành công. Vui lòng thử lại.";
            return RedirectToAction(nameof(Login));
        }

        var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? email;

        if (string.IsNullOrEmpty(email))
        {
            TempData["LoginError"] = "Không lấy được email từ Google. Vui lòng thử lại.";
            return RedirectToAction(nameof(Login));
        }

        // ── Tài khoản đã tồn tại → đăng nhập luôn ──
        var user = await _context.NhanViens.Include(n => n.ChucVu)
            .FirstOrDefaultAsync(x => x.Email == email);

        if (user != null)
        {
            // Tự liên kết GoogleId nếu chưa có
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleId;
                await _context.SaveChangesAsync();
            }

            if (!user.TrangThai)
            {
                TempData["LoginError"] = "Tài khoản đã bị khóa. Vui lòng liên hệ thư viện.";
                return RedirectToAction(nameof(Login));
            }

            if (user.MaChucVu == "STU" && !string.IsNullOrEmpty(user.MaDocGia))
            {
                var dg = await _context.DocGias.FindAsync(user.MaDocGia);
                if (dg != null && !dg.DaDuyet)
                {
                    TempData["LoginError"] = "Tài khoản của bạn đang chờ thủ thư xác nhận.";
                    return RedirectToAction(nameof(Login));
                }
            }

            await SignInUserAsync(user);
            return user.MaChucVu is "ADMIN" or "LIB"
                ? RedirectToAction("Index", "Staff")
                : RedirectToAction("Index", "Student");
        }

        // ── Tài khoản chưa tồn tại → luồng đăng ký mới ──
        // Xóa OTP cũ của email này nếu có
        var oldOtps = _context.GoogleOtpTemps.Where(o => o.Email == email);
        _context.GoogleOtpTemps.RemoveRange(oldOtps);

        string otp = GenerateOtp();
        _context.GoogleOtpTemps.Add(new GoogleOtpTemp
        {
            Email = email,
            GoogleId = googleId,
            HoTen = name,
            OtpCode = otp,
            OtpExpiry = DateTime.Now.AddMinutes(10)
        });
        await _context.SaveChangesAsync();

        // Chỉ lưu email/name vào TempData (không lưu OTP - dễ bị mất)
        TempData["GoogleEmail"] = email;
        TempData["GoogleName"] = name;

        try
        {
            await _emailService.SendOtpAsync(email, name, otp, "register");
        }
        catch
        {
            TempData["LoginError"] = "Không gửi được email xác minh. Vui lòng thử lại hoặc đăng ký bằng email thường.";
            return RedirectToAction(nameof(Login));
        }

        return RedirectToAction(nameof(GoogleVerifyOtp));
    }

    // Bước 3: Nhập OTP đã gửi về email Google
    public IActionResult GoogleVerifyOtp()
    {
        var email = TempData.Peek("GoogleEmail") as string;
        if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Login));

        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");

        ViewBag.Email = email;
        ViewBag.Name = TempData.Peek("GoogleName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleVerifyOtp(string OtpInput)
    {
        var email = TempData.Peek("GoogleEmail") as string;

        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");

        if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Login));

        ViewBag.Email = email;
        ViewBag.Name = TempData.Peek("GoogleName");

        // Đọc OTP từ DB — không phụ thuộc TempData/Cookie/Session
        var otpRecord = await _context.GoogleOtpTemps
            .FirstOrDefaultAsync(o => o.Email == email);

        if (otpRecord == null)
        {
            ViewBag.Error = "Phiên đăng ký đã hết hạn. Vui lòng bấm Gửi lại.";
            return View();
        }

        if (otpRecord.OtpCode != OtpInput?.Trim() || DateTime.Now > otpRecord.OtpExpiry)
        {
            ViewBag.Error = "Mã OTP không đúng hoặc đã hết hạn. Vui lòng gửi lại.";
            return View();
        }

        // OTP đúng → lưu GoogleId vào TempData rồi mới xóa record
        TempData["GoogleId"] = otpRecord.GoogleId;
        _context.GoogleOtpTemps.Remove(otpRecord);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(GoogleLinkCard));
    }

    // Bước 4 (gửi lại OTP nếu hết hạn)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleResendOtp()
    {
        var email = TempData.Peek("GoogleEmail") as string;
        var name = TempData.Peek("GoogleName") as string;

        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");

        if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Login));

        // Xóa OTP cũ, tạo mới trong DB
        var oldOtps = _context.GoogleOtpTemps.Where(o => o.Email == email);
        _context.GoogleOtpTemps.RemoveRange(oldOtps);

        string otp = GenerateOtp();
        _context.GoogleOtpTemps.Add(new GoogleOtpTemp
        {
            Email = email,
            GoogleId = TempData.Peek("GoogleId") as string ?? "",
            HoTen = name ?? email,
            OtpCode = otp,
            OtpExpiry = DateTime.Now.AddMinutes(10)
        });
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendOtpAsync(email, name ?? email, otp, "register");
            TempData["GoogleResendSuccess"] = $"Đã gửi lại mã OTP về {email}.";
        }
        catch
        {
            TempData["GoogleResendError"] = "Lỗi gửi email. Vui lòng thử lại.";
        }

        return RedirectToAction(nameof(GoogleVerifyOtp));
    }

    // Bước 5: Nhập mã thẻ sinh viên để hoàn tất đăng ký
    public IActionResult GoogleLinkCard()
    {
        var email = TempData.Peek("GoogleEmail") as string;
        if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Login));

        TempData.Keep("GoogleId");
        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");

        ViewBag.Email = email;
        ViewBag.Name = TempData.Peek("GoogleName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleLinkCard(string MaTheTV, string? HoTen, string? Lop, string? Khoa, string? SoDienThoai)
    {
        var googleId = TempData.Peek("GoogleId") as string;
        var email = TempData.Peek("GoogleEmail") as string;
        var name = !string.IsNullOrWhiteSpace(HoTen) ? HoTen!.Trim() : (TempData.Peek("GoogleName") as string);

        TempData.Keep("GoogleId");
        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            return RedirectToAction(nameof(Login));

        ViewBag.Email = email;
        ViewBag.Name = name;

        if (string.IsNullOrWhiteSpace(MaTheTV))
        {
            ViewBag.Error = "Vui lòng nhập mã thẻ thư viện.";
            return View();
        }

        // Kiểm tra mã thẻ
        var the = await _context.TheThiViens
            .Include(t => t.DocGia)
            .FirstOrDefaultAsync(t => t.MaThe == MaTheTV.ToUpper().Trim());

        if (the == null) { ViewBag.Error = "Mã thẻ thư viện không tồn tại."; return View(); }
        if (!the.TrangThai) { ViewBag.Error = "Thẻ đã bị vô hiệu hóa. Liên hệ thủ thư."; return View(); }
        if (the.MaDocGia != null) { ViewBag.Error = "Thẻ này đã được liên kết với tài khoản khác."; return View(); }

        // Email Google đã được đăng ký → cho phép nhập email khác
        if (await _context.NhanViens.AnyAsync(n => n.Email == email))
        {
            TempData.Keep("GoogleId");
            TempData.Keep("GoogleEmail");
            TempData.Keep("GoogleName");
            TempData["EmailExists"] = email;
            TempData["PendingMaThe"] = MaTheTV.ToUpper().Trim();
            TempData["PendingLop"] = Lop;
            TempData["PendingKhoa"] = Khoa;
            TempData["PendingSoDT"] = SoDienThoai;
            return RedirectToAction(nameof(GoogleChangeEmail));
        }

        if (!await _context.ChucVus.AnyAsync(c => c.MaChucVu == "STU"))
            _context.ChucVus.Add(new ChucVu { MaChucVu = "STU", TenChucVu = "Sinh Viên" });

        var newMaDG = await GenerateMaDGAsync();
        var docGia = new DocGia
        {
            MaDocGia = newMaDG,
            HoTen = name ?? email,
            Lop = Lop,
            Khoa = Khoa,
            Email = email,
            SoDienThoai = SoDienThoai,
            NgayTaoThe = DateTime.Now,
            TrangThaiThe = true,
            MaTheTV = MaTheTV.ToUpper().Trim(),
            DaDuyet = false,
            AnhDaiDien = the.AnhThe
        };
        the.MaDocGia = newMaDG;
        _context.DocGias.Add(docGia);

        var newMaNV = await GenerateMaNVAsync();
        // Mật khẩu ngẫu nhiên — tài khoản Google không dùng login mật khẩu
        var randomPw = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

        _context.NhanViens.Add(new NhanVien
        {
            MaNV = newMaNV,
            HoTen = name ?? email,
            Email = email,
            SoDienThoai = SoDienThoai,
            MatKhau = randomPw,
            MatKhauTuDat = false, // tài khoản Google — chưa từng tự đặt mật khẩu thật
            MaChucVu = "STU",
            TrangThai = true,
            MaDocGia = newMaDG,
            NgayTao = DateTime.Now,
            EmailVerified = true,   // Google đã xác minh email rồi
            GoogleId = googleId
        });

        await _context.SaveChangesAsync();

        TempData.Remove("GoogleId");
        TempData.Remove("GoogleEmail");
        TempData.Remove("GoogleName");
        TempData.Remove("GoogleOtp");
        TempData.Remove("GoogleOtpExpiry");

        TempData["VerifySuccess"] = true;
        return RedirectToAction(nameof(Login));
    }

    // ── Trang nhập email khác khi email Google đã tồn tại ──
    public IActionResult GoogleChangeEmail()
    {
        var emailExists = TempData.Peek("EmailExists") as string;
        if (string.IsNullOrEmpty(emailExists)) return RedirectToAction(nameof(Login));

        TempData.Keep("GoogleId");
        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");
        TempData.Keep("EmailExists");
        TempData.Keep("PendingMaThe");
        TempData.Keep("PendingLop");
        TempData.Keep("PendingKhoa");
        TempData.Keep("PendingSoDT");

        ViewBag.GoogleEmail = TempData.Peek("GoogleEmail");
        ViewBag.ExistsEmail = emailExists;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleChangeEmail(string NewEmail)
    {
        var googleId = TempData.Peek("GoogleId") as string;
        var googleEmail = TempData.Peek("GoogleEmail") as string;
        var name = TempData.Peek("GoogleName") as string;
        var maThe = TempData.Peek("PendingMaThe") as string;
        var lop = TempData.Peek("PendingLop") as string;
        var khoa = TempData.Peek("PendingKhoa") as string;
        var soDT = TempData.Peek("PendingSoDT") as string;

        TempData.Keep("GoogleId");
        TempData.Keep("GoogleEmail");
        TempData.Keep("GoogleName");
        TempData.Keep("EmailExists");
        TempData.Keep("PendingMaThe");
        TempData.Keep("PendingLop");
        TempData.Keep("PendingKhoa");
        TempData.Keep("PendingSoDT");

        ViewBag.GoogleEmail = googleEmail;
        ViewBag.ExistsEmail = TempData.Peek("EmailExists");

        if (string.IsNullOrWhiteSpace(NewEmail) || !NewEmail.Contains('@'))
        {
            ViewBag.Error = "Vui lòng nhập địa chỉ email hợp lệ.";
            return View();
        }

        NewEmail = NewEmail.Trim().ToLower();

        if (await _context.NhanViens.AnyAsync(n => n.Email == NewEmail))
        {
            ViewBag.Error = $"Email {NewEmail} cũng đã được đăng ký. Vui lòng dùng email khác.";
            return View();
        }

        // Xác nhận thẻ vẫn còn hợp lệ
        var the = await _context.TheThiViens.Include(t => t.DocGia)
            .FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the == null || !the.TrangThai || the.MaDocGia != null)
        {
            ViewBag.Error = "Thẻ thư viện không còn hợp lệ. Vui lòng quay lại.";
            return View();
        }

        if (!await _context.ChucVus.AnyAsync(c => c.MaChucVu == "STU"))
            _context.ChucVus.Add(new ChucVu { MaChucVu = "STU", TenChucVu = "Sinh Viên" });

        var newMaDG = await GenerateMaDGAsync();
        var docGia = new DocGia
        {
            MaDocGia = newMaDG,
            HoTen = name ?? NewEmail,
            Lop = lop,
            Khoa = khoa,
            Email = NewEmail,
            SoDienThoai = soDT,
            NgayTaoThe = DateTime.Now,
            TrangThaiThe = true,
            MaTheTV = maThe,
            DaDuyet = false,
            AnhDaiDien = the.AnhThe
        };
        the.MaDocGia = newMaDG;
        _context.DocGias.Add(docGia);

        var newMaNV = await GenerateMaNVAsync();
        var randomPw = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

        _context.NhanViens.Add(new NhanVien
        {
            MaNV = newMaNV,
            HoTen = name ?? NewEmail,
            Email = NewEmail,
            SoDienThoai = soDT,
            MatKhau = randomPw,
            MatKhauTuDat = false, // tài khoản Google — chưa từng tự đặt mật khẩu thật
            MaChucVu = "STU",
            TrangThai = true,
            MaDocGia = newMaDG,
            NgayTao = DateTime.Now,
            EmailVerified = true,
            GoogleId = googleId
        });

        await _context.SaveChangesAsync();

        TempData.Remove("GoogleId"); TempData.Remove("GoogleEmail");
        TempData.Remove("GoogleName"); TempData.Remove("EmailExists");
        TempData.Remove("PendingMaThe"); TempData.Remove("PendingLop");
        TempData.Remove("PendingKhoa"); TempData.Remove("PendingSoDT");

        TempData["VerifySuccess"] = true;
        return RedirectToAction(nameof(Login));
    }

    // ── LOGOUT ────────────────────────────────────────────
    public async Task<IActionResult> Logout()
    {
        var maNV = User.FindFirst("MaNV")?.Value;
        var hoTen = User.FindFirst(ClaimTypes.Name)?.Value;
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

    // ── API kiểm tra mã thẻ (AJAX) ────────────────────────
    [HttpGet]
    public async Task<IActionResult> CheckCard(string ma)
    {
        if (string.IsNullOrWhiteSpace(ma))
            return Json(new { ok = false, message = "Vui lòng nhập mã thẻ." });

        var the = await _context.TheThiViens
            .Include(t => t.DocGia)
            .FirstOrDefaultAsync(t => t.MaThe == ma.ToUpper().Trim());

        if (the == null) return Json(new { ok = false, message = "Mã thẻ không tồn tại trên hệ thống." });
        if (!the.TrangThai) return Json(new { ok = false, message = "Thẻ thư viện này đã bị vô hiệu hóa." });
        if (the.MaDocGia != null) return Json(new { ok = false, message = "Thẻ này đã được liên kết với tài khoản khác." });

        return Json(new { ok = true, message = $"Thẻ hợp lệ. Hết hạn: {the.NgayHetHan:dd/MM/yyyy}" });
    }

    // ── HELPER ────────────────────────────────────────────
    private static string GenerateOtp()
    {
        // Dùng RandomNumberGenerator thay cho new Random() — an toàn hơn
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private async Task<string> GenerateMaDGAsync()
    {
        var last = await _context.DocGias.OrderByDescending(d => d.MaDocGia)
            .Select(d => d.MaDocGia).FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(last) && last.StartsWith("DG") && int.TryParse(last[2..], out int n))
            return "DG" + (n + 1).ToString("D3");
        return "DG001";
    }

    private async Task<string> GenerateMaNVAsync()
    {
        var last = await _context.NhanViens.OrderByDescending(n => n.MaNV)
            .Select(n => n.MaNV).FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(last) && last.StartsWith("NV") && int.TryParse(last[2..], out int n))
            return "NV" + (n + 1).ToString("D3");
        return "NV001";
    }

    private async Task SignInUserAsync(NhanVien user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,  user.HoTen),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role,  user.MaChucVu ?? ""),
            new("MaNV",           user.MaNV),
            new("MaDocGia",       user.MaDocGia ?? ""),
            new("LaAdmin",        user.LaAdmin ? "true" : "false")
        };
        // Nhân viên được bật "Là Admin" ở màn Phân quyền sẽ có thêm role ADMIN,
        // dù chức vụ (MaChucVu) của họ không phải ADMIN — để [Authorize(Roles="ADMIN")]
        // ở các trang khác trong hệ thống tự động cho họ full quyền.
        if (user.LaAdmin && user.MaChucVu != "ADMIN")
            claims.Add(new Claim(ClaimTypes.Role, "ADMIN"));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        _context.NhatKyHoatDongs.Add(new NhatKyHoatDong
        {
            MaNV = user.MaNV,
            HanhDong = "Đăng nhập",
            MoTa = $"{user.HoTen} ({user.MaChucVu}) đăng nhập vào hệ thống lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
            ThoiGian = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }
    public IActionResult TaoHash()
    {
        return Content(BCrypt.Net.BCrypt.HashPassword("123456"));
    }
}