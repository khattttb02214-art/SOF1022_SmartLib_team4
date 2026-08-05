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

        // Chỉ tính sách đang hoạt động (TrangThai = true) — đúng với những gì
        // trang "Quản lý kho" đang hiển thị, sách đã ngừng hoạt động (ẩn khỏi kho) không tính.
        model.TongSach = await _context.Saches.CountAsync(s => s.TrangThai);
        model.TongDocGia = await _context.DocGias.CountAsync();
        model.TongNhanVien = await _context.NhanViens.CountAsync();
        model.SachDangMuon = await _context.MuonTras
            .CountAsync(x => x.TrangThai == "Đang Mượn");
        model.SachQuaHan = await _context.MuonTras
            .CountAsync(x => x.TrangThai == "Đang Mượn" && x.NgayHenTra < DateTime.Now);

        // Độc giả đã được duyệt tài khoản
        model.DocGiaDaDuyet = await _context.DocGias.CountAsync(d => d.DaDuyet);

        // Tổng số cuốn (bản sao) đang "Có Sẵn" trong kho — có thể cho mượn ngay
        model.SachCoSan = await _context.Saches
            .Where(s => s.TrangThai)
            .SumAsync(s => s.SoLuongKhaDung);

        // Đơn đặt trước đang chờ duyệt
        model.DonDatTruocChoDuyet = await _context.Reservations
            .CountAsync(r => r.TrangThai == "Đang Chờ");

        // ── Top 5 phiếu mượn quá hạn (quá hạn lâu nhất lên đầu) ──
        var phieuQuaHan = await _context.MuonTras
            .Include(m => m.DocGia)
            .Include(m => m.ChiTietMuonTras).ThenInclude(ct => ct.Sach)
            .Where(m => m.TrangThai == "Đang Mượn" && m.NgayHenTra < DateTime.Now)
            .OrderBy(m => m.NgayHenTra)
            .Take(5)
            .ToListAsync();

        model.PhieuMuonQuaHan = phieuQuaHan.Select(m => new PhieuQuaHanItem
        {
            MaPhieu = m.MaPhieu,
            TenDocGia = m.DocGia?.HoTen ?? "—",
            TenSachHienThi = MoTaDanhSachSach(m.ChiTietMuonTras.Select(ct => ct.Sach?.TenSach)),
            NgayHenTra = m.NgayHenTra,
            SoNgayQuaHan = (int)Math.Max(0, (DateTime.Now.Date - m.NgayHenTra.Date).TotalDays)
        }).ToList();

        // ── Top 5 đơn đặt trước đang chờ duyệt (đặt sớm nhất lên đầu) ──
        var donChoDuyet = await _context.Reservations
            .Include(r => r.DocGia)
            .Include(r => r.ChiTietDatTruocs).ThenInclude(ct => ct.Sach)
            .Where(r => r.TrangThai == "Đang Chờ")
            .OrderBy(r => r.NgayDat)
            .Take(5)
            .ToListAsync();

        model.DonDatTruocCanDuyet = donChoDuyet.Select(r => new DatTruocChoDuyetItem
        {
            MaReservation = r.MaReservation,
            TenDocGia = r.DocGia?.HoTen ?? "—",
            TenSachHienThi = MoTaDanhSachSach(r.ChiTietDatTruocs.Select(ct => ct.Sach?.TenSach)),
            NgayDat = r.NgayDat,
            TrangThai = r.TrangThai ?? "Đang Chờ"
        }).ToList();

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

    // Một phiếu mượn / đơn đặt trước có thể gồm nhiều đầu sách khác nhau.
    // Hiển thị tên cuốn đầu tiên, kèm "+N sách khác" nếu còn lại.
    private static string MoTaDanhSachSach(IEnumerable<string?> tenSachs)
    {
        var list = tenSachs.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!).ToList();
        if (list.Count == 0) return "—";
        if (list.Count == 1) return list[0];
        return $"{list[0]} +{list.Count - 1} sách khác";
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

