using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.Services;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

/// <summary>
/// Màn hình phân quyền chi tiết cho nhân viên: cho phép ADMIN cấp/thu hồi độc lập 4 quyền
/// Xem / Thêm / Sửa / Xóa cho từng chức năng, theo từng nhân viên (không theo chức vụ),
/// nên 2 nhân viên cùng chức vụ vẫn có thể có bộ quyền khác nhau.
/// </summary>
[Authorize(Roles = "ADMIN")]
public class PhanQuyenController : Controller
{
    private readonly SmartLibDbContext _context;
    private readonly AuditService _auditService;

    public PhanQuyenController(SmartLibDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // ── Trang chính ───────────────────────────────────────────────
    public async Task<IActionResult> Index(string? maNV)
    {
        var danhSachNV = await _context.NhanViens
            .Include(n => n.ChucVu)
            .Where(n => n.MaChucVu != "STU")
            .OrderBy(n => n.HoTen)
            .ToListAsync();

        if (string.IsNullOrEmpty(maNV) || !danhSachNV.Any(n => n.MaNV == maNV))
            maNV = danhSachNV.FirstOrDefault()?.MaNV;

        var vm = new PhanQuyenIndexViewModel
        {
            DanhSachNhanVien = danhSachNV,
            MaNVDangChon = maNV
        };

        if (!string.IsNullOrEmpty(maNV))
        {
            var (laAdmin, groups) = await BuildMatrixAsync(maNV);
            vm.LaAdmin = laAdmin;
            vm.NhomChucNangs = groups;
        }

        return View(vm);
    }

    // ── AJAX: lấy ma trận quyền của 1 nhân viên (khi đổi lựa chọn hoặc "sao chép quyền") ──
    [HttpGet]
    public async Task<IActionResult> GetMatrix(string maNV)
    {
        if (string.IsNullOrEmpty(maNV)) return BadRequest(new { success = false, message = "Thiếu mã nhân viên." });

        var nv = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(n => n.MaNV == maNV);
        if (nv == null) return NotFound(new { success = false, message = "Không tìm thấy nhân viên." });

        var (laAdmin, groups) = await BuildMatrixAsync(maNV);
        return Json(new
        {
            success = true,
            laAdmin,
            laAdminTheoChucVu = nv.MaChucVu == "ADMIN",
            hoTen = nv.HoTen,
            groups
        });
    }

    // ── AJAX: lưu toàn bộ ma trận quyền của 1 nhân viên ─────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] PhanQuyenSaveModel? model)
    {
        if (model == null || string.IsNullOrEmpty(model.MaNV))
            return Json(new { success = false, message = "Dữ liệu gửi lên không hợp lệ." });

        var nv = await _context.NhanViens.FindAsync(model.MaNV);
        if (nv == null)
            return Json(new { success = false, message = "Không tìm thấy nhân viên." });

        nv.LaAdmin = model.LaAdmin;

        var existingRows = await _context.PhanQuyenNhanViens
            .Where(p => p.MaNV == model.MaNV)
            .ToListAsync();
        var existingMap = existingRows.ToDictionary(p => p.MaChucNang);

        foreach (var item in model.Quyens)
        {
            if (existingMap.TryGetValue(item.MaChucNang, out var row))
            {
                row.DuocXem = item.Xem;
                row.DuocThem = item.Them;
                row.DuocSua = item.Sua;
                row.DuocXoa = item.Xoa;
            }
            else if (item.Xem || item.Them || item.Sua || item.Xoa)
            {
                _context.PhanQuyenNhanViens.Add(new PhanQuyenNhanVien
                {
                    MaNV = model.MaNV,
                    MaChucNang = item.MaChucNang,
                    DuocXem = item.Xem,
                    DuocThem = item.Them,
                    DuocSua = item.Sua,
                    DuocXoa = item.Xoa
                });
            }
        }

        await _context.SaveChangesAsync();

        var adminId = User.FindFirst("MaNV")?.Value ?? "NV001";
        var ghiChuAdmin = model.LaAdmin ? " — đã bật LÀ ADMIN (toàn quyền hệ thống)" : "";
        await _auditService.LogAsync(adminId, "Phân quyền",
            $"Cập nhật phân quyền cho nhân viên {nv.HoTen} ({nv.MaNV}){ghiChuAdmin}");

        return Json(new { success = true, message = $"Đã lưu phân quyền cho {nv.HoTen}." });
    }

    // ── Dựng dữ liệu ma trận quyền (nhóm > chức năng > 4 cờ quyền) cho 1 nhân viên ──
    private async Task<(bool laAdmin, List<NhomChucNangDto> groups)> BuildMatrixAsync(string maNV)
    {
        var nv = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(n => n.MaNV == maNV);

        var nhoms = await _context.NhomChucNangs
            .Include(g => g.ChucNangs)
            .AsNoTracking()
            .OrderBy(g => g.ThuTu)
            .ToListAsync();

        var perms = await _context.PhanQuyenNhanViens
            .Where(p => p.MaNV == maNV)
            .AsNoTracking()
            .ToListAsync();
        var permMap = perms.ToDictionary(p => p.MaChucNang);

        var groups = nhoms.Select(g => new NhomChucNangDto
        {
            MaNhom = g.MaNhom,
            TenNhom = g.TenNhom,
            Icon = g.Icon,
            ChucNangs = g.ChucNangs.OrderBy(c => c.ThuTu).Select(c =>
            {
                permMap.TryGetValue(c.MaChucNang, out var p);
                return new ChucNangQuyenDto
                {
                    MaChucNang = c.MaChucNang,
                    TenChucNang = c.TenChucNang,
                    Icon = c.Icon ?? g.Icon,
                    Xem = p?.DuocXem ?? false,
                    Them = p?.DuocThem ?? false,
                    Sua = p?.DuocSua ?? false,
                    Xoa = p?.DuocXoa ?? false
                };
            }).ToList()
        }).ToList();

        return (nv?.LaAdmin ?? false, groups);
    }
}
