using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Attributes;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.Services;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

/// <summary>
/// Màn "Quản lý đánh giá sách" dành cho ADMIN + Thủ thư: xem/tìm/lọc toàn bộ đánh
/// giá của sinh viên, xem chi tiết, và ẩn (soft-delete) các đánh giá vi phạm
/// (spam, ngôn từ phản cảm, quảng cáo, nội dung không liên quan...) mà không xóa
/// dữ liệu khỏi CSDL — chỉ đổi TrangThai để có thể khôi phục lại nếu ẩn nhầm.
///
/// Controller này chỉ ánh xạ vào DUY NHẤT 1 dòng ChucNang ("Quản lý đánh giá
/// sách" — xem SqlScripts/20260726_AddDanhGiaSachManagement.sql) nên không cần
/// gắn [ThuocChucNang] cho action nào.
/// </summary>
[Authorize(Roles = "ADMIN,LIB")]
public class AdminReviewController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly AuditService _auditService;

    public AdminReviewController(SmartLibDbContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    private string? MaNV => User.FindFirst("MaNV")?.Value;

    // ── DANH SÁCH + TÌM KIẾM + LỌC ─────────────────────────────────
    public async Task<IActionResult> Index(string? search, int? soSao, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
    {
        var query = _db.DanhGiaSaches
            .Include(d => d.Sach)
            .Include(d => d.DocGia)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(d =>
                d.MaDanhGia.ToString().Contains(kw) ||
                (d.Sach != null && d.Sach.TenSach.Contains(kw)) ||
                (d.DocGia != null && d.DocGia.HoTen.Contains(kw)) ||
                (d.DocGia != null && d.DocGia.Email != null && d.DocGia.Email.Contains(kw)) ||
                (d.NoiDung != null && d.NoiDung.Contains(kw)));
        }

        if (soSao.HasValue) query = query.Where(d => d.SoSao == soSao.Value);
        if (!string.IsNullOrEmpty(trangThai)) query = query.Where(d => d.TrangThai == trangThai);
        if (tuNgay.HasValue) query = query.Where(d => d.NgayDanhGia >= tuNgay.Value);
        if (denNgay.HasValue) query = query.Where(d => d.NgayDanhGia <= denNgay.Value.AddDays(1));

        var danhSach = await query.OrderByDescending(d => d.NgayDanhGia).ToListAsync();

        var vm = new AdminReviewViewModel
        {
            DanhSach = danhSach,
            Search = search,
            SoSao = soSao,
            TrangThai = trangThai,
            TuNgay = tuNgay,
            DenNgay = denNgay,
            TongSoDanhGia = await _db.DanhGiaSaches.CountAsync(),
            SoDangHienThi = await _db.DanhGiaSaches.CountAsync(d => d.TrangThai == "Hiển thị"),
            SoDaXoa = await _db.DanhGiaSaches.CountAsync(d => d.TrangThai == "Đã xóa"),
            DiemTrungBinh = await _db.DanhGiaSaches
                .Where(d => d.TrangThai == "Hiển thị")
                .Select(d => (double?)d.SoSao)
                .AverageAsync() ?? 0
        };

        return View(vm);
    }

    // ── AJAX: Xem chi tiết 1 đánh giá ───────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var dg = await _db.DanhGiaSaches
            .Include(d => d.Sach).ThenInclude(s => s!.TheLoai)
            .Include(d => d.DocGia)
            .FirstOrDefaultAsync(d => d.MaDanhGia == id);

        if (dg == null)
            return Json(new { success = false, message = "Không tìm thấy đánh giá." });

        return Json(new
        {
            success = true,
            maDanhGia = dg.MaDanhGia,
            soSao = dg.SoSao,
            noiDung = dg.NoiDung,
            ngayDanhGia = dg.NgayDanhGia.ToString("HH:mm dd/MM/yyyy"),
            trangThai = dg.TrangThai,
            sach = new
            {
                maSach = dg.MaSach,
                tenSach = dg.Sach?.TenSach,
                anhBia = dg.Sach?.AnhBia,
                theLoai = dg.Sach?.TheLoai?.TenTheLoai
            },
            docGia = new
            {
                maDocGia = dg.MaDocGia,
                hoTen = dg.DocGia?.HoTen,
                email = dg.DocGia?.Email,
                lop = dg.DocGia?.Lop
            }
        });
    }

    // ── Ẩn (soft-delete) 1 đánh giá vi phạm ─────────────────────────
    // Tên action chứa "xoa" → PhanQuyenActionFilter tự suy ra quyền "Xóa".
    public async Task<IActionResult> XoaMem(int id)
    {
        var dg = await _db.DanhGiaSaches.FirstOrDefaultAsync(d => d.MaDanhGia == id);
        if (dg == null)
        {
            TempData["error"] = "Không tìm thấy đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        dg.TrangThai = "Đã xóa";
        await _db.SaveChangesAsync();

        var maNV = MaNV;
        if (!string.IsNullOrEmpty(maNV))
        {
            await _auditService.LogAsync(maNV, "Xóa đánh giá",
                $"Ẩn đánh giá #{dg.MaDanhGia} ({dg.SoSao} sao, sách {dg.MaSach}).");
        }

        TempData["success"] = "Đã ẩn đánh giá khỏi hệ thống (dữ liệu vẫn được giữ lại).";
        return RedirectToAction(nameof(Index));
    }

    // ── Khôi phục hiển thị 1 đánh giá đã ẩn ──────────────────────────
    // Gắn tường minh quyền "Xóa" (giống hành động ẩn) vì "khoiphuc" không khớp
    // quy ước đặt tên nào — để mặc định suy luận theo tên sẽ ra "Xem" (sai ý muốn).
    [ChucNangQuyen(LoaiQuyen.Xoa)]
    public async Task<IActionResult> KhoiPhuc(int id)
    {
        var dg = await _db.DanhGiaSaches.FirstOrDefaultAsync(d => d.MaDanhGia == id);
        if (dg == null)
        {
            TempData["error"] = "Không tìm thấy đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        dg.TrangThai = "Hiển thị";
        await _db.SaveChangesAsync();

        var maNV = MaNV;
        if (!string.IsNullOrEmpty(maNV))
        {
            await _auditService.LogAsync(maNV, "Khôi phục đánh giá",
                $"Khôi phục hiển thị đánh giá #{dg.MaDanhGia} (sách {dg.MaSach}).");
        }

        TempData["success"] = "Đã khôi phục hiển thị đánh giá.";
        return RedirectToAction(nameof(Index));
    }
}
