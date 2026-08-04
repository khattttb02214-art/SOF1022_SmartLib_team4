using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class StaffController : Controller
{
    private readonly SmartLibDbContext _context;

    public StaffController(SmartLibDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeViewModel();

        model.TongSach = await _context.Saches.CountAsync();
        model.TongDocGia = await _context.DocGias.CountAsync();
        model.TongNhanVien = await _context.NhanViens.CountAsync();
        model.SachDangMuon = await _context.MuonTras
            .CountAsync(x => x.TrangThai == "Đang Mượn");
        model.SachQuaHan = await _context.MuonTras
            .CountAsync(x => x.TrangThai == "Đang Mượn" && x.NgayHenTra < DateTime.Now);

        model.SachMoiNhat = await _context.Saches
            .Include(s => s.TheLoai)
            .Where(s => s.TrangThai == true)
            .OrderByDescending(s => s.NgayTao)
            .Take(6)
            .Select(s => new SachMoiItem
            {
                MaSach = s.MaSach,
                TenSach = s.TenSach,
                TenTheLoai = s.TheLoai != null ? s.TheLoai.TenTheLoai : "Chưa phân loại",
                AnhBia = s.AnhBia,
                SoLuongKhaDung = s.SoLuongKhaDung
            })
            .ToListAsync();

        model.DanhSachTheLoai = await _context.TheLoais
            .Select(t => new TheLoaiItem
            {
                MaTheLoai = t.MaTheLoai,
                TenTheLoai = t.TenTheLoai,
                SoLuongSach = _context.Saches.Count(s => s.MaTheLoai == t.MaTheLoai)
            })
            .ToListAsync();

        model.MuonTraGanDay = await _context.MuonTras
            .Include(m => m.DocGia)
            .OrderByDescending(m => m.NgayMuon)
            .Take(5)
            .Select(m => new MuonTraGanDayItem
            {
                MaPhieu = m.MaPhieu,
                TenDocGia = m.DocGia != null ? m.DocGia.HoTen : "—",
                NgayMuon = m.NgayMuon,
                NgayHenTra = m.NgayHenTra,
                TrangThai = m.TrangThai ?? "—"
            })
            .ToListAsync();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var maNV = User.FindFirst("MaNV")?.Value;

        if (string.IsNullOrEmpty(maNV))
            return RedirectToAction("Login", "Auth");

        var nhanVien = await _context.NhanViens
            .Include(x => x.ChucVu)
            .FirstOrDefaultAsync(x => x.MaNV == maNV);

        if (nhanVien == null)
            return NotFound();

        return View(nhanVien);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(
        string HoTen,
        string? SoDienThoai,
        string? DiaChi,
        string? NewEmail,
        IFormFile? AnhDaiDienFile)
    {
        var maNV = User.FindFirst("MaNV")?.Value;

        if (string.IsNullOrEmpty(maNV))
            return RedirectToAction("Login", "Auth");

        var nhanVien = await _context.NhanViens
            .FirstOrDefaultAsync(x => x.MaNV == maNV);

        if (nhanVien == null)
            return NotFound();

        // Cập nhật thông tin
        nhanVien.HoTen = HoTen;
        nhanVien.SoDienThoai = SoDienThoai;
        nhanVien.DiaChi = DiaChi;
        nhanVien.NgayCapNhat = DateTime.Now;

        // Tạm thời đổi email trực tiếp
        // Sau này thay bằng OTP
        if (!string.IsNullOrWhiteSpace(NewEmail))
        {
            bool tonTai = await _context.NhanViens
                .AnyAsync(x => x.Email == NewEmail && x.MaNV != maNV);

            if (tonTai)
            {
                ViewBag.Error = "Email đã tồn tại.";
                nhanVien.ChucVu = await _context.ChucVus
                    .FirstOrDefaultAsync(x => x.MaChucVu == nhanVien.MaChucVu);
                return View(nhanVien);
            }

            nhanVien.Email = NewEmail;
        }

        // Upload ảnh đại diện
        if (AnhDaiDienFile != null && AnhDaiDienFile.Length > 0)
        {
            string folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "staff");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString()
                            + Path.GetExtension(AnhDaiDienFile.FileName);

            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await AnhDaiDienFile.CopyToAsync(stream);
            }

            nhanVien.AnhDaiDien = "/uploads/staff/" + fileName;
        }

        await _context.SaveChangesAsync();

        TempData["success"] = "Cập nhật thông tin thành công.";

        return RedirectToAction(nameof(EditProfile));
    }
}
    
