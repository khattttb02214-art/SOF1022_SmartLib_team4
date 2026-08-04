using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Services;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "STU")]
public class StudentController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly EmailService _emailService;

    public StudentController(SmartLibDbContext db, IWebHostEnvironment env, EmailService emailService)
    { _db = db; _env = env; _emailService = emailService; }

    string? MaDocGia => User.FindFirst("MaDocGia")?.Value;
    string? MaNV => User.FindFirst("MaNV")?.Value;

    public async Task<IActionResult> Index()
    {
        var model = new StudentHomeViewModel();

        // Load sách mới nhất, kèm CuonSaches để tính số lượng thực tế
        var sachList = await _db.Saches
            .Include(s => s.TheLoai)
            .Include(s => s.CuonSaches)
            .Where(s => s.TrangThai)
            .OrderByDescending(s => s.NgayTao)
            .Take(8)
            .ToListAsync();

        // Sync SoLuongKhaDung nếu bị lệch, rồi gán vào ViewModel
        bool anyChanged = false;
        foreach (var s in sachList)
        {
            int coSan = s.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
            if (s.SoLuongKhaDung != coSan)
            {
                s.SoLuongKhaDung = coSan;
                anyChanged = true;
            }
        }
        if (anyChanged) await _db.SaveChangesAsync();

        model.SachMoiNhat = sachList.Select(s => new SachMoiItem
        {
            MaSach = s.MaSach,
            TenSach = s.TenSach,
            TenTheLoai = s.TheLoai?.TenTheLoai ?? "Chưa phân loại",
            AnhBia = s.AnhBia,
            // Dùng số đã sync (từ CuonSach) - luôn chính xác
            SoLuongKhaDung = s.SoLuongKhaDung
        }).ToList();

        model.DanhSachTheLoai = await _db.TheLoais
            .Select(t => new TheLoaiItem
            {
                MaTheLoai = t.MaTheLoai,
                TenTheLoai = t.TenTheLoai,
                SoLuongSach = _db.Saches.Count(s => s.MaTheLoai == t.MaTheLoai)
            })
            .ToListAsync();

        // Dùng CuonSach để tính tổng sẵn có (nguồn sự thật)
        model.TongSach = await _db.Saches.CountAsync(s => s.TrangThai);
        model.TongTheLoai = await _db.TheLoais.CountAsync();
        model.SachKhaDung = await _db.CuonSaches
            .Where(c => c.TrangThai == "Có Sẵn" && c.Sach != null && c.Sach.TrangThai)
            .Select(c => c.MaSach)
            .Distinct()
            .CountAsync();

        // Phiếu mượn đang mượn của sinh viên
        if (!string.IsNullOrEmpty(MaDocGia))
        {
            model.PhieuDangMuon = await _db.MuonTras
                .Include(m => m.ChiTietMuonTras)
                    .ThenInclude(ct => ct.Sach)
                .Where(m => m.MaDocGia == MaDocGia && m.TrangThai == "Đang Mượn")
                .OrderByDescending(m => m.NgayMuon)
                .ToListAsync();
        }

        return View(model);
    }

    // ── Lịch sử mượn ─────────────────────────────────────────────────────────
    public async Task<IActionResult> LichSuMuon()
    {
        if (string.IsNullOrEmpty(MaDocGia)) return RedirectToAction(nameof(Index));
        var list = await _db.MuonTras
            .Include(m => m.ChiTietMuonTras)
                .ThenInclude(ct => ct.Sach)
            .Where(m => m.MaDocGia == MaDocGia)
            .OrderByDescending(m => m.NgayMuon)
            .ToListAsync();
        return View(list);
    }

    // ── Sửa thông tin cá nhân ─────────────────────────────────────────────────
    public async Task<IActionResult> EditProfile()
    {
        if (string.IsNullOrEmpty(MaDocGia)) return RedirectToAction(nameof(Index));
        var dg = await _db.DocGias.FindAsync(MaDocGia);
        if (dg == null) return NotFound();
        ViewBag.DocGia = dg;
        ViewBag.DaDatMatKhau = (await _db.NhanViens.FindAsync(MaNV))?.MatKhauTuDat ?? false;
        return View(dg);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(
        string HoTen, string? SoDienThoai, string? DiaChi, IFormFile? AnhDaiDienFile)
    {
        if (string.IsNullOrEmpty(MaDocGia)) return RedirectToAction(nameof(Index));
        var dg = await _db.DocGias.FindAsync(MaDocGia);
        if (dg == null) return NotFound();

        var nv = await _db.NhanViens.FirstOrDefaultAsync(n => n.MaDocGia == MaDocGia);
        if (nv == null) return NotFound();

        string otp = new Random().Next(100000, 999999).ToString();
        nv.OtpCode = otp;
        nv.OtpExpiry = DateTime.Now.AddMinutes(10);
        await _db.SaveChangesAsync();

        HttpContext.Session.SetString("pending_HoTen", HoTen);
        HttpContext.Session.SetString("pending_SoDienThoai", SoDienThoai ?? "");
        HttpContext.Session.SetString("pending_DiaChi", DiaChi ?? "");

        TempData["OtpSent"] = $"OTP đã được gửi về email {dg.Email}. (Dev mode: {otp})";
        return RedirectToAction(nameof(ConfirmOtp));
    }

    // ── ĐẶT / ĐỔI MẬT KHẨU ĐĂNG NHẬP ──────────────────────────────────────
    // Áp dụng cho cả tài khoản đăng ký Google (đặt mật khẩu LẦN ĐẦU để có thể
    // đăng nhập bằng email+mật khẩu) lẫn tài khoản đăng ký thường (đổi mật khẩu).
    // Luôn bắt buộc xác nhận qua OTP gửi về email trước khi cho sửa.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiOtpDoiMatKhau()
    {
        if (string.IsNullOrEmpty(MaNV)) return RedirectToAction(nameof(EditProfile));
        var nv = await _db.NhanViens.FindAsync(MaNV);
        if (nv == null || string.IsNullOrEmpty(nv.Email)) return RedirectToAction(nameof(EditProfile));

        string otp = GenerateOtpAnToan();
        nv.OtpResetMatKhau = otp;
        nv.OtpResetMatKhauHetHan = DateTime.Now.AddMinutes(10);
        await _db.SaveChangesAsync();
        await _emailService.SendOtpAsync(nv.Email, nv.HoTen, otp, "reset_password");

        TempData["success"] = $"Mã xác nhận đã được gửi tới email {nv.Email}.";
        return RedirectToAction(nameof(DoiMatKhau));
    }

    public async Task<IActionResult> DoiMatKhau()
    {
        if (string.IsNullOrEmpty(MaNV)) return RedirectToAction(nameof(EditProfile));
        var nv = await _db.NhanViens.FindAsync(MaNV);
        if (nv == null) return RedirectToAction(nameof(EditProfile));
        if (string.IsNullOrEmpty(nv.OtpResetMatKhau))
        {
            TempData["error"] = "Vui lòng bấm \"Đặt/Đổi mật khẩu\" ở trang hồ sơ để nhận mã xác nhận trước.";
            return RedirectToAction(nameof(EditProfile));
        }
        ViewBag.DaDatMatKhau = nv.MatKhauTuDat;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(string otp, string matKhauMoi, string xacNhanMatKhau)
    {
        if (string.IsNullOrEmpty(MaNV)) return RedirectToAction(nameof(EditProfile));
        var nv = await _db.NhanViens.FindAsync(MaNV);
        if (nv == null) return RedirectToAction(nameof(EditProfile));
        ViewBag.DaDatMatKhau = nv.MatKhauTuDat;

        if (string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(matKhauMoi))
        { ViewBag.Error = "Vui lòng nhập đầy đủ mã xác nhận và mật khẩu mới"; return View(); }
        if (matKhauMoi.Length < 6)
        { ViewBag.Error = "Mật khẩu phải ít nhất 6 ký tự"; return View(); }
        if (matKhauMoi != xacNhanMatKhau)
        { ViewBag.Error = "Mật khẩu xác nhận không khớp"; return View(); }
        if (nv.OtpResetMatKhau != otp || nv.OtpResetMatKhauHetHan == null || nv.OtpResetMatKhauHetHan < DateTime.Now)
        { ViewBag.Error = "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng gửi lại mã."; return View(); }

        nv.MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);
        nv.MatKhauTuDat = true;
        nv.OtpResetMatKhau = null;
        nv.OtpResetMatKhauHetHan = null;
        await _db.SaveChangesAsync();

        TempData["success"] = "Đặt mật khẩu thành công! Từ giờ bạn có thể đăng nhập bằng email và mật khẩu này.";
        return RedirectToAction(nameof(EditProfile));
    }

    private static string GenerateOtpAnToan() =>
        System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public IActionResult ConfirmOtp() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmOtp(string OtpInput)
    {
        if (string.IsNullOrEmpty(MaDocGia) || string.IsNullOrEmpty(MaNV))
            return RedirectToAction(nameof(Index));

        var nv = await _db.NhanViens.FindAsync(MaNV);
        var dg = await _db.DocGias.FindAsync(MaDocGia);
        if (nv == null || dg == null) return NotFound();

        if (nv.OtpCode != OtpInput || nv.OtpExpiry < DateTime.Now)
        {
            TempData["error"] = "OTP không đúng hoặc đã hết hạn";
            return View();
        }

        dg.HoTen = HttpContext.Session.GetString("pending_HoTen") ?? dg.HoTen;
        dg.SoDienThoai = HttpContext.Session.GetString("pending_SoDienThoai");
        dg.DiaChi = HttpContext.Session.GetString("pending_DiaChi");
        nv.HoTen = dg.HoTen;
        nv.OtpCode = null;
        nv.OtpExpiry = null;
        await _db.SaveChangesAsync();

        HttpContext.Session.Remove("pending_HoTen");
        HttpContext.Session.Remove("pending_SoDienThoai");
        HttpContext.Session.Remove("pending_DiaChi");

        TempData["success"] = "Cập nhật thông tin thành công";
        return RedirectToAction(nameof(Index));
    }
}