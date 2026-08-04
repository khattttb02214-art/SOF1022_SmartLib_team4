using BCrypt.Net;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Attributes;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class NhanVienController : Controller
{
    private readonly SmartLibDbContext _context;
    private readonly IWebHostEnvironment _env;

    public NhanVienController(SmartLibDbContext context, IWebHostEnvironment env)
    { _context = context; _env = env; }

    // Chỉ ADMIN thấy danh sách nhân viên
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Index(string? search)
    {
        var q = _context.NhanViens.Include(n => n.ChucVu).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(n => n.HoTen.Contains(search) || (n.Email != null && n.Email.Contains(search)));
        ViewBag.Search = search;
        return View(await q.OrderBy(n => n.HoTen).ToListAsync());
    }

    // Thủ thư chỉ được tạo tài khoản sinh viên
    // Admin được tạo mọi loại tài khoản
    public async Task<IActionResult> Create()
    {
        await LoadCV();
        return View(new NhanVienViewModel());
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NhanVienViewModel model)
    {
        // Kiểm tra: Thủ thư chỉ được tạo tài khoản STU
        if (User.IsInRole("LIB") && !User.IsInRole("ADMIN"))
        {
            if (model.MaChucVu != "STU")
            {
                TempData["error"] = "Thủ thư chỉ có quyền tạo tài khoản sinh viên";
                await LoadCV();
                return View(model);
            }
        }

        if (!ModelState.IsValid) { await LoadCV(); return View(model); }
        if (string.IsNullOrWhiteSpace(model.MatKhau)) { ModelState.AddModelError("MatKhau","Mật khẩu không được trống"); await LoadCV(); return View(model); }
        if (await _context.NhanViens.AnyAsync(n => n.Email == model.Email)) { ModelState.AddModelError("Email","Email đã tồn tại"); await LoadCV(); return View(model); }

        var last = await _context.NhanViens.OrderByDescending(n => n.MaNV).Select(n => n.MaNV).FirstOrDefaultAsync();
        string maNV = "NV001";
        if (!string.IsNullOrEmpty(last) && last.StartsWith("NV") && int.TryParse(last[2..], out int num))
            maNV = "NV" + (num + 1).ToString("D3");

        string? avatar = null;
        if (model.AnhDaiDienFile != null)
        {
            avatar = Guid.NewGuid() + Path.GetExtension(model.AnhDaiDienFile.FileName);
            var folder = Path.Combine(_env.WebRootPath, "uploads/staff");
            Directory.CreateDirectory(folder);
            await using var fs = new FileStream(Path.Combine(folder, avatar), FileMode.Create);
            await model.AnhDaiDienFile.CopyToAsync(fs);
        }

        _context.NhanViens.Add(new NhanVien {
            MaNV = maNV, HoTen = model.HoTen, Email = model.Email,
            SoDienThoai = model.SoDienThoai, DiaChi = model.DiaChi,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau!),
            MaChucVu = model.MaChucVu, TrangThai = model.TrangThai,
            AnhDaiDien = avatar, NgayTao = DateTime.Now
        });
        await _context.SaveChangesAsync();
        TempData["success"] = "Thêm tài khoản thành công";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Edit(string id)
    {
        var nv = await _context.NhanViens.FindAsync(id);
        if (nv == null) return NotFound();
        await LoadCV();
        return View(new NhanVienViewModel {
            MaNV = nv.MaNV, HoTen = nv.HoTen, Email = nv.Email!,
            SoDienThoai = nv.SoDienThoai, DiaChi = nv.DiaChi,
            MaChucVu = nv.MaChucVu, TrangThai = nv.TrangThai
        });
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, NhanVienViewModel model)
    {
        var nv = await _context.NhanViens.FindAsync(id);
        if (nv == null) return NotFound();

        nv.HoTen = model.HoTen; nv.SoDienThoai = model.SoDienThoai;
        nv.DiaChi = model.DiaChi; nv.MaChucVu = model.MaChucVu;
        nv.TrangThai = model.TrangThai; nv.NgayCapNhat = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(model.MatKhau))
            nv.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

        if (model.AnhDaiDienFile != null)
        {
            var avatar = Guid.NewGuid() + Path.GetExtension(model.AnhDaiDienFile.FileName);
            var folder = Path.Combine(_env.WebRootPath, "uploads/staff");
            Directory.CreateDirectory(folder);
            await using var fs = new FileStream(Path.Combine(folder, avatar), FileMode.Create);
            await model.AnhDaiDienFile.CopyToAsync(fs);
            nv.AnhDaiDien = avatar;
        }

        await _context.SaveChangesAsync();
        TempData["success"] = "Cập nhật nhân viên thành công";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(string id)
    {
        var nv = await _context.NhanViens.FindAsync(id);
        if (nv == null) return NotFound();
        if (nv.MaNV == "NV001") { TempData["error"] = "Không thể xóa tài khoản Admin gốc"; return RedirectToAction(nameof(Index)); }

        // Nhân viên đã từng lập/xử lý phiếu mượn thì KHÔNG thể xóa (ràng buộc khóa
        // ngoại MuonTra → NhanVien là Restrict, xóa sẽ vỡ dữ liệu lịch sử mượn trả).
        if (await _context.MuonTras.AnyAsync(m => m.MaNV == id))
        {
            TempData["error"] = $"Không thể xóa: nhân viên {nv.HoTen} đã từng lập/xử lý phiếu mượn trong hệ thống. " +
                "Bạn có thể dùng nút \"Khóa tài khoản\" để ngừng cho đăng nhập mà vẫn giữ nguyên dữ liệu, thay vì xóa hẳn.";
            return RedirectToAction(nameof(Index));
        }

        _context.NhanViens.Remove(nv);
        await _context.SaveChangesAsync();
        TempData["success"] = "Đã xóa nhân viên";
        return RedirectToAction(nameof(Index));
    }

    // Đổi trạng thái (Hoạt động ⇄ Đã khóa) THAY VÌ xóa hẳn — dữ liệu (phiếu mượn đã
    // xử lý, nhật ký hoạt động...) vẫn được giữ nguyên, chỉ khóa không cho đăng nhập.
    public async Task<IActionResult> ToggleStatus(string id)
    {
        if (id == "NV001") { TempData["error"] = "Không thể khóa tài khoản Admin gốc"; return RedirectToAction(nameof(Index)); }
        var nv = await _context.NhanViens.FindAsync(id);
        if (nv == null) return NotFound();
        nv.TrangThai = !nv.TrangThai;
        nv.NgayCapNhat = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["success"] = nv.TrangThai ? "Đã kích hoạt lại tài khoản nhân viên" : "Đã khóa tài khoản (ẩn quyền đăng nhập, giữ nguyên dữ liệu)";
        return RedirectToAction(nameof(Index));
    }

    // ── Duyệt tài khoản sinh viên (ADMIN + LIB) ──────────
    [Authorize(Roles = "ADMIN,LIB")]
    [ThuocChucNang(12)] // Duyệt tài khoản đăng ký — tách riêng khỏi "Quản lý nhân viên" (cùng Controller=NhanVien)
    public async Task<IActionResult> PendingAccounts()
    {
        var pending = await _context.DocGias
            .Where(d => !d.DaDuyet)
            .OrderByDescending(d => d.NgayTaoThe)
            .ToListAsync();
        return View(pending);
    }

    [Authorize(Roles = "ADMIN,LIB")]
    [ThuocChucNang(12)] // Duyệt tài khoản đăng ký
    public async Task<IActionResult> ApproveAccount(string id)
    {
        var dg = await _context.DocGias.FindAsync(id);
        if (dg == null) return NotFound();
        dg.DaDuyet = true;
        await _context.SaveChangesAsync();

        // Log activity
        var maNV = User.FindFirst("MaNV")?.Value;
        if (!string.IsNullOrEmpty(maNV))
            await GhiNhatKy(maNV, "Duyệt tài khoản", $"Đã duyệt tài khoản sinh viên {dg.HoTen} ({dg.MaDocGia})");

        TempData["success"] = $"Đã duyệt tài khoản của {dg.HoTen}";
        return RedirectToAction(nameof(PendingAccounts));
    }

    [Authorize(Roles = "ADMIN,LIB")]
    [ThuocChucNang(12)] // Duyệt tài khoản đăng ký
    public async Task<IActionResult> RejectAccount(string id)
    {
        var dg = await _context.DocGias
            .Include(d => d.TheThiViens)
            .FirstOrDefaultAsync(d => d.MaDocGia == id);

        if (dg == null) return NotFound();

        var maNV = User.FindFirst("MaNV")?.Value;
        var tenSV = dg.HoTen;

        // Gỡ liên kết thẻ thư viện (không xóa thẻ, chỉ bỏ MaDocGia)
        foreach (var the in dg.TheThiViens)
        {
            the.MaDocGia = null;
        }

        // Xóa NhanVien STU liên kết
        var nv = await _context.NhanViens
            .Include(n => n.NhatKyHoatDongs)
            .FirstOrDefaultAsync(n => n.MaDocGia == id);

        if (nv != null)
        {
            // Xóa nhật ký liên quan
            _context.NhatKyHoatDongs.RemoveRange(nv.NhatKyHoatDongs);
            _context.NhanViens.Remove(nv);
        }

        _context.DocGias.Remove(dg);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(maNV))
            await GhiNhatKy(maNV, "Từ chối tài khoản", $"Đã từ chối yêu cầu đăng ký của sinh viên {tenSV}");

        TempData["success"] = "Đã từ chối và xóa yêu cầu đăng ký";
        return RedirectToAction(nameof(PendingAccounts));
    }

    private async Task GhiNhatKy(string maNV, string hanhDong, string moTa)
    {
        _context.NhatKyHoatDongs.Add(new NhatKyHoatDong {
            MaNV = maNV, HanhDong = hanhDong, MoTa = moTa, ThoiGian = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    private async Task LoadCV()
    {
        IQueryable<ChucVu> query = _context.ChucVus.Where(c => c.MaChucVu != "STU");
        
        // Thủ thư chỉ được tạo tài khoản sinh viên
        if (User.IsInRole("LIB") && !User.IsInRole("ADMIN"))
        {
            // Chỉ cho phép role STU
            query = _context.ChucVus.Where(c => c.MaChucVu == "STU");
        }
        
        ViewBag.ChucVu = new SelectList(await query.ToListAsync(), "MaChucVu", "TenChucVu");
    }

    // ── API: lấy số lượng thông báo cho chuông Bell (ADMIN + LIB) ──
    [Authorize(Roles = "ADMIN,LIB")]
    [HttpGet]
    [BoQuaPhanQuyen] // Gộp số liệu từ nhiều chức năng khác nhau (tài khoản chờ duyệt +
                      // phiếu quá hạn), không thuộc riêng 1 chức năng nên không chặn theo quyền.
    public async Task<IActionResult> GetNotificationCounts()
    {
        // Đếm tài khoản sinh viên chờ duyệt
        var pendingCount = await _context.DocGias
            .CountAsync(d => !d.DaDuyet);

        // Đếm phiếu mượn quá hạn
        var overdueCount = await _context.MuonTras
            .CountAsync(m => m.TrangThai == "Đang Mượn" && m.NgayHenTra < DateTime.Now);

        return Json(new { pendingCount, overdueCount });
    }
}
