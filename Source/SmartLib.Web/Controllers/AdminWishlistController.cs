using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.Services;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

/// <summary>
/// Màn "Quản lý Wishlist" dành cho ADMIN + Thủ thư: tổng hợp wishlist/sở thích của
/// toàn bộ sinh viên để nắm nhu cầu đọc, phát hiện sách đang "hot" mà hết hàng, và
/// chủ động gửi gợi ý sách tới đúng sinh viên quan tâm.
///
/// Controller này chỉ ánh xạ vào DUY NHẤT 1 dòng ChucNang ("Quản lý Wishlist" —
/// xem SqlScripts/20260724_AddAdminWishlist.sql) nên không cần gắn [ThuocChucNang]
/// cho action nào — PhanQuyenActionFilter tự khớp theo tên Controller.
/// </summary>
[Authorize(Roles = "ADMIN,LIB")]
public class AdminWishlistController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly AuditService _auditService;

    public AdminWishlistController(SmartLibDbContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    private string? MaNV => User.FindFirst("MaNV")?.Value;

    // ── TRANG TỔNG QUAN ──────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? searchSinhVien)
    {
        var vm = new AdminWishlistViewModel { SearchSinhVien = searchSinhVien };

        // --- KPI tổng quan ---
        vm.TongLuotYeuThich = await _db.Wishlists.CountAsync();

        vm.TongSachDuocQuanTam = await _db.Wishlists
            .Where(w => w.MaSach != null)
            .Select(w => w.MaSach)
            .Distinct()
            .CountAsync();

        var svTuWishlist = _db.Wishlists.Where(w => w.MaDocGia != null).Select(w => w.MaDocGia!);
        var svTuSoThich = _db.WishlistPreferences.Select(p => p.MaDocGia);
        vm.TongSinhVienThamGia = await svTuWishlist.Union(svTuSoThich).Distinct().CountAsync();

        vm.TongGoiYDaGui = await _db.ThongBaos.CountAsync(t => t.LoaiThongBao == "GOI_Y_THU_THU");

        // --- Top sách được yêu thích nhiều nhất (chỉ xét sách còn đang hoạt động) ---
        var topSachRaw = await _db.Wishlists
            .Where(w => w.MaSach != null)
            .GroupBy(w => w.MaSach)
            .Select(g => new { MaSach = g.Key!, SoLuong = g.Count() })
            .OrderByDescending(g => g.SoLuong)
            .Take(12)
            .ToListAsync();

        var maSachTop = topSachRaw.Select(x => x.MaSach).ToList();
        var sachTop = await _db.Saches
            .Include(s => s.TheLoai)
            .Include(s => s.NhaXuatBan)
            .Where(s => maSachTop.Contains(s.MaSach) && s.TrangThai)
            .ToListAsync();
        var sachTopMap = sachTop.ToDictionary(s => s.MaSach);

        vm.TopSachYeuThich = topSachRaw
            .Where(x => sachTopMap.ContainsKey(x.MaSach))
            .Select(x => new SachYeuThichNhieuDto { Sach = sachTopMap[x.MaSach], SoLuotYeuThich = x.SoLuong })
            .ToList();

        // --- Thống kê sở thích phổ biến theo Thể loại / Tác giả / NXB ---
        vm.TopTheLoai = await ThongKeSoThichAsync("THELOAI");
        vm.TopTacGia = await ThongKeSoThichAsync("TACGIA");
        vm.TopNXB = await ThongKeSoThichAsync("NXB");

        // --- Wishlist theo từng sinh viên (chỉ liệt kê SV có hoạt động wishlist) ---
        var docGiaQ = _db.DocGias.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchSinhVien))
        {
            var kw = searchSinhVien.Trim();
            docGiaQ = docGiaQ.Where(d => d.HoTen.Contains(kw) || d.MaDocGia.Contains(kw));
        }

        var wishlistTheoSV = await _db.Wishlists
            .Where(w => w.MaDocGia != null)
            .GroupBy(w => w.MaDocGia)
            .Select(g => new { MaDocGia = g.Key!, SoLuong = g.Count(), LanCuoi = g.Max(x => x.NgayThem) })
            .ToListAsync();
        var wishlistMap = wishlistTheoSV.ToDictionary(x => x.MaDocGia);

        var soThichTheoSV = await _db.WishlistPreferences
            .GroupBy(p => p.MaDocGia)
            .Select(g => new { MaDocGia = g.Key, SoLuong = g.Count() })
            .ToListAsync();
        var soThichMap = soThichTheoSV.ToDictionary(x => x.MaDocGia);

        var dsSinhVien = await docGiaQ.OrderBy(d => d.HoTen).ToListAsync();
        vm.DanhSachSinhVien = dsSinhVien
            .Where(d => wishlistMap.ContainsKey(d.MaDocGia) || soThichMap.ContainsKey(d.MaDocGia))
            .Select(d => new SinhVienWishlistDto
            {
                DocGia = d,
                SoSachYeuThich = wishlistMap.TryGetValue(d.MaDocGia, out var w) ? w.SoLuong : 0,
                SoSoThich = soThichMap.TryGetValue(d.MaDocGia, out var p) ? p.SoLuong : 0,
                LanCuoiThem = wishlistMap.TryGetValue(d.MaDocGia, out var w2) ? w2.LanCuoi : (DateTime?)null
            })
            .OrderByDescending(x => x.SoSachYeuThich)
            .ThenByDescending(x => x.SoSoThich)
            .ToList();

        // --- Lịch sử gợi ý đã gửi (mới nhất trước, tối đa 50 dòng gần nhất) ---
        vm.LichSuGoiY = await _db.ThongBaos
            .Include(t => t.DocGia)
            .Include(t => t.Sach)
            .Include(t => t.NhanVien)
            .Where(t => t.LoaiThongBao == "GOI_Y_THU_THU")
            .OrderByDescending(t => t.NgayTao)
            .Take(50)
            .ToListAsync();

        return View(vm);
    }

    /// <summary>Thống kê top 8 MaRef được nhiều SV theo dõi nhất cho 1 loại sở thích, kèm tên hiển thị.</summary>
    private async Task<List<ThongKeSoThichDto>> ThongKeSoThichAsync(string loai)
    {
        var grouped = await _db.WishlistPreferences
            .Where(p => p.LoaiSoThich == loai)
            .GroupBy(p => p.MaRef)
            .Select(g => new { MaRef = g.Key, SoLuong = g.Count() })
            .OrderByDescending(g => g.SoLuong)
            .Take(8)
            .ToListAsync();

        if (!grouped.Any()) return new List<ThongKeSoThichDto>();

        var maRefs = grouped.Select(g => g.MaRef).ToList();
        Dictionary<string, string> tenMap = loai switch
        {
            "THELOAI" => await _db.TheLoais.Where(t => maRefs.Contains(t.MaTheLoai)).ToDictionaryAsync(t => t.MaTheLoai, t => t.TenTheLoai),
            "TACGIA" => await _db.TacGias.Where(t => maRefs.Contains(t.MaTacGia)).ToDictionaryAsync(t => t.MaTacGia, t => t.TenTacGia),
            "NXB" => await _db.NhaXuatBans.Where(t => maRefs.Contains(t.MaNXB)).ToDictionaryAsync(t => t.MaNXB, t => t.TenNXB),
            _ => new Dictionary<string, string>()
        };

        return grouped.Select(g => new ThongKeSoThichDto
        {
            MaRef = g.MaRef,
            Ten = tenMap.TryGetValue(g.MaRef, out var ten) ? ten : g.MaRef,
            SoLuotTheoDoi = g.SoLuong
        }).ToList();
    }

    // ── AJAX: Tìm sách để mở modal "Gợi ý sách" ───────────────────────────
    [HttpGet]
    public async Task<IActionResult> TimSach(string? q)
    {
        var query = _db.Saches
            .Include(s => s.TheLoai)
            .Where(s => s.TrangThai)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim();
            query = query.Where(s => s.TenSach.Contains(kw)
                || s.MaSach.Contains(kw)
                || (s.ISBN != null && s.ISBN.Contains(kw)));
        }

        var sachList = await query.OrderByDescending(s => s.NgayTao).Take(20).ToListAsync();
        var maSachList = sachList.Select(s => s.MaSach).ToList();

        var wishlistCounts = await _db.Wishlists
            .Where(w => w.MaSach != null && maSachList.Contains(w.MaSach))
            .GroupBy(w => w.MaSach)
            .Select(g => new { MaSach = g.Key!, SoLuong = g.Count() })
            .ToDictionaryAsync(x => x.MaSach, x => x.SoLuong);

        var items = sachList.Select(s => new
        {
            maSach = s.MaSach,
            tenSach = s.TenSach,
            theLoai = s.TheLoai?.TenTheLoai,
            anhBia = s.AnhBia,
            soLuongKho = s.SoLuongKho,
            soLuongKhaDung = s.SoLuongKhaDung,
            soLuotYeuThich = wishlistCounts.TryGetValue(s.MaSach, out var c) ? c : 0
        }).ToList();

        return Json(new { success = true, items });
    }

    // ── AJAX: Danh sách người nhận gợi ý cho 1 sách cụ thể ────────────────
    // Trả về 2 nhóm: (1) SV đã yêu thích đúng sách này, (2) SV có sở thích
    // (thể loại/tác giả/NXB) khớp với sách nhưng chưa yêu thích sách này.
    [HttpGet]
    public async Task<IActionResult> LayNguoiNhan(string maSach)
    {
        var sach = await _db.Saches
            .Include(s => s.TheLoai)
            .Include(s => s.NhaXuatBan)
            .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
            .FirstOrDefaultAsync(s => s.MaSach == maSach);

        if (sach == null)
            return Json(new { success = false, message = "Không tìm thấy sách." });

        var daYeuThichRaw = await _db.Wishlists
            .Include(w => w.DocGia)
            .Where(w => w.MaSach == maSach && w.DocGia != null)
            .ToListAsync();

        var daYeuThich = daYeuThichRaw
            .Where(w => w.DocGia != null)
            .Select(w => new { maDocGia = w.DocGia!.MaDocGia, hoTen = w.DocGia.HoTen, lop = w.DocGia.Lop })
            .GroupBy(x => x.maDocGia)
            .Select(g => g.First())
            .OrderBy(x => x.hoTen)
            .ToList();

        var daYeuThichIds = daYeuThich.Select(x => x.maDocGia).ToHashSet();

        // Sở thích phù hợp: cùng thể loại / cùng NXB / cùng 1 trong các tác giả của sách
        var maTacGiaList = sach.SachTacGias.Select(st => st.MaTacGia).ToList();

        var phuHopRaw = await _db.WishlistPreferences
            .Include(p => p.DocGia)
            .Where(p =>
                (p.LoaiSoThich == "THELOAI" && sach.MaTheLoai != null && p.MaRef == sach.MaTheLoai) ||
                (p.LoaiSoThich == "NXB" && sach.MaNXB != null && p.MaRef == sach.MaNXB) ||
                (p.LoaiSoThich == "TACGIA" && maTacGiaList.Contains(p.MaRef)))
            .ToListAsync();

        string TenLyDo(string loai, string maRef) => loai switch
        {
            "THELOAI" => $"Thể loại: {sach.TheLoai?.TenTheLoai ?? maRef}",
            "NXB" => $"NXB: {sach.NhaXuatBan?.TenNXB ?? maRef}",
            "TACGIA" => $"Tác giả: {sach.SachTacGias.FirstOrDefault(st => st.MaTacGia == maRef)?.TacGia?.TenTacGia ?? maRef}",
            _ => maRef
        };

        var phuHopSoThich = phuHopRaw
            .Where(p => p.DocGia != null && !daYeuThichIds.Contains(p.MaDocGia))
            .GroupBy(p => p.MaDocGia)
            .Select(g => new
            {
                maDocGia = g.Key,
                hoTen = g.First().DocGia!.HoTen,
                lop = g.First().DocGia!.Lop,
                lyDo = string.Join(", ", g.Select(x => TenLyDo(x.LoaiSoThich, x.MaRef)).Distinct())
            })
            .OrderBy(x => x.hoTen)
            .ToList();

        return Json(new
        {
            success = true,
            tenSach = sach.TenSach,
            anhBia = sach.AnhBia,
            daYeuThich,
            phuHopSoThich
        });
    }

    // ── AJAX: Chi tiết wishlist của 1 sinh viên (xem trước khi gợi ý riêng) ──
    [HttpGet]
    public async Task<IActionResult> ChiTietDocGia(string maDocGia)
    {
        var dg = await _db.DocGias.FirstOrDefaultAsync(d => d.MaDocGia == maDocGia);
        if (dg == null)
            return Json(new { success = false, message = "Không tìm thấy sinh viên." });

        var wishlist = await _db.Wishlists
            .Include(w => w.Sach).ThenInclude(s => s!.TheLoai)
            .Where(w => w.MaDocGia == maDocGia)
            .OrderByDescending(w => w.NgayThem)
            .ToListAsync();

        var soThich = await _db.WishlistPreferences
            .Where(p => p.MaDocGia == maDocGia)
            .ToListAsync();

        var theLoaiIds = soThich.Where(p => p.LoaiSoThich == "THELOAI").Select(p => p.MaRef).ToList();
        var tacGiaIds = soThich.Where(p => p.LoaiSoThich == "TACGIA").Select(p => p.MaRef).ToList();
        var nxbIds = soThich.Where(p => p.LoaiSoThich == "NXB").Select(p => p.MaRef).ToList();

        var tenTheLoai = await _db.TheLoais.Where(t => theLoaiIds.Contains(t.MaTheLoai)).ToDictionaryAsync(t => t.MaTheLoai, t => t.TenTheLoai);
        var tenTacGia = await _db.TacGias.Where(t => tacGiaIds.Contains(t.MaTacGia)).ToDictionaryAsync(t => t.MaTacGia, t => t.TenTacGia);
        var tenNxb = await _db.NhaXuatBans.Where(t => nxbIds.Contains(t.MaNXB)).ToDictionaryAsync(t => t.MaNXB, t => t.TenNXB);

        string TenSoThich(string loai, string maRef) => loai switch
        {
            "THELOAI" => tenTheLoai.GetValueOrDefault(maRef, maRef),
            "TACGIA" => tenTacGia.GetValueOrDefault(maRef, maRef),
            "NXB" => tenNxb.GetValueOrDefault(maRef, maRef),
            _ => maRef
        };

        return Json(new
        {
            success = true,
            hoTen = dg.HoTen,
            lop = dg.Lop,
            khoa = dg.Khoa,
            wishlist = wishlist.Select(w => new
            {
                maSach = w.MaSach,
                tenSach = w.Sach?.TenSach,
                anhBia = w.Sach?.AnhBia,
                theLoai = w.Sach?.TheLoai?.TenTheLoai,
                ngayThem = w.NgayThem.ToString("dd/MM/yyyy")
            }),
            soThich = soThich.Select(p => new { loai = p.LoaiSoThich, ten = TenSoThich(p.LoaiSoThich, p.MaRef) })
        });
    }

    // ── Gửi gợi ý sách tới 1 hoặc nhiều sinh viên ─────────────────────────
    // Tên action chứa "tao" → PhanQuyenActionFilter tự suy ra quyền "Thêm".
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoGoiY([FromBody] GuiGoiYRequest? model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.MaSach) || model.MaDocGias == null || !model.MaDocGias.Any())
            return Json(new { success = false, message = "Vui lòng chọn sách và ít nhất 1 sinh viên để gửi gợi ý." });

        var sach = await _db.Saches.FindAsync(model.MaSach);
        if (sach == null)
            return Json(new { success = false, message = "Không tìm thấy sách." });

        // Chỉ gửi cho độc giả có thật trong hệ thống, tránh rác nếu client gửi id sai
        var docGiaHopLe = await _db.DocGias
            .Where(d => model.MaDocGias.Contains(d.MaDocGia))
            .Select(d => d.MaDocGia)
            .ToListAsync();

        if (!docGiaHopLe.Any())
            return Json(new { success = false, message = "Danh sách sinh viên không hợp lệ." });

        var loiNhan = model.LoiNhan?.Trim();
        var noiDung = string.IsNullOrWhiteSpace(loiNhan)
            ? $"Thư viện gợi ý bạn đọc cuốn «{sach.TenSach}» — có thể bạn sẽ thích đấy!"
            : loiNhan;
        if (noiDung.Length > 500) noiDung = noiDung.Substring(0, 500);

        var maNV = MaNV;
        foreach (var maDG in docGiaHopLe)
        {
            _db.ThongBaos.Add(new ThongBao
            {
                MaDocGia = maDG,
                MaSach = model.MaSach,
                TieuDe = "Gợi ý sách từ thủ thư",
                NoiDung = noiDung,
                LoaiThongBao = "GOI_Y_THU_THU",
                MaNV = maNV,
                DaDoc = false,
                NgayTao = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(maNV))
        {
            await _auditService.LogAsync(maNV, "Gợi ý sách",
                $"Gửi gợi ý sách «{sach.TenSach}» ({sach.MaSach}) cho {docGiaHopLe.Count} sinh viên.");
        }

        return Json(new { success = true, message = $"Đã gửi gợi ý cho {docGiaHopLe.Count} sinh viên." });
    }

    // ── Thu hồi 1 gợi ý đã gửi ─────────────────────────────────────────────
    // Tên action chứa "xoa" → PhanQuyenActionFilter tự suy ra quyền "Xóa".
    public async Task<IActionResult> XoaGoiY(int id)
    {
        var tb = await _db.ThongBaos.FirstOrDefaultAsync(t => t.MaThongBao == id && t.LoaiThongBao == "GOI_Y_THU_THU");
        if (tb != null)
        {
            _db.ThongBaos.Remove(tb);
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã thu hồi gợi ý.";
        }
        return RedirectToAction(nameof(Index));
    }
}
