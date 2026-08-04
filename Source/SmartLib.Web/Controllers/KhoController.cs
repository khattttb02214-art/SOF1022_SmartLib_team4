using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class KhoController : Controller
{
    private readonly SmartLibDbContext _db;
    public KhoController(SmartLibDbContext db) => _db = db;

    // ── INDEX: Tổng quan kho sách ────────────────────────────────
    public async Task<IActionResult> Index(string? filter)
    {
        var sachs = await _db.Saches
            .Include(s => s.TheLoai)
            .Include(s => s.KeSach)
            .Include(s => s.CuonSaches)
            .Where(s => s.TrangThai)
            .ToListAsync();

        var list = sachs.Select(s => new KhoViewModel
        {
            MaSach = s.MaSach,
            TenSach = s.TenSach,
            AnhBia = s.AnhBia,
            TenTheLoai = s.TheLoai?.TenTheLoai,
            SoLuongKho = s.SoLuongKho,
            SoLuongKhaDung = s.SoLuongKhaDung,
            DangMuon = s.CuonSaches.Count(c => c.TrangThai == "Đang Mượn"),
            TrangThai = s.SoLuongKhaDung == 0 ? "Hết sách" :
                             s.SoLuongKhaDung < 3 ? "Sắp hết" : "Còn hàng"
        }).AsQueryable();

        if (filter == "het") list = list.Where(k => k.TrangThai == "Hết sách");
        if (filter == "saphet") list = list.Where(k => k.TrangThai == "Sắp hết");
        if (filter == "muon") list = list.Where(k => k.DangMuon > 0);

        ViewBag.Filter = filter;
        ViewBag.TongSach = list.Count();
        ViewBag.HetSach = list.Count(k => k.TrangThai == "Hết sách");
        ViewBag.SapHet = list.Count(k => k.TrangThai == "Sắp hết");
        ViewBag.DangMuon = list.Sum(k => k.DangMuon);

        return View(list.OrderBy(k => k.TrangThai).ThenBy(k => k.TenSach).ToList());
    }

    // ── KỆ SÁCH INDEX ────────────────────────────────────────────
    public async Task<IActionResult> KeSach(string? tang, string? phong)
    {
        var keList = await _db.KeSaches
            .Include(k => k.NXBPhuTrach)
            .Include(k => k.TheLoaiPhuTrach)
            .Include(k => k.Saches).ThenInclude(s => s.CuonSaches)
            .ToListAsync();

        // Thống kê
        var vmList = keList.Select(k => new KeSachViewModel
        {
            MaKe = k.MaKe,
            TenKe = k.TenKe,
            ViTri = k.ViTri,
            Tang = k.Tang,
            Phong = k.Phong,
            MoTa = k.MoTa,
            SucChua = k.SucChua,
            MaNXBPhuTrach = k.MaNXBPhuTrach,
            MaTheLoaiPhuTrach = k.MaTheLoaiPhuTrach,
            TrangThai = k.TrangThai,
            SoSach = k.Saches.Count(s => s.TrangThai),
            SoCuon = k.Saches.Where(s => s.TrangThai).Sum(s => s.CuonSaches.Count),
            TenNXBPhuTrach = k.NXBPhuTrach?.TenNXB,
            TenTheLoaiPhuTrach = k.TheLoaiPhuTrach?.TenTheLoai,
        }).AsQueryable();

        if (tang != null && int.TryParse(tang, out var t))
            vmList = vmList.Where(k => k.Tang == t);
        if (!string.IsNullOrEmpty(phong))
            vmList = vmList.Where(k => k.Phong == phong);

        ViewBag.DanhSachTang = keList.Where(k => k.Tang.HasValue).Select(k => k.Tang).Distinct().OrderBy(t => t).ToList();
        ViewBag.DanhSachPhong = keList.Where(k => !string.IsNullOrEmpty(k.Phong)).Select(k => k.Phong).Distinct().OrderBy(p => p).ToList();
        ViewBag.FilterTang = tang;
        ViewBag.FilterPhong = phong;

        ViewBag.TongKe = vmList.Count();
        ViewBag.TongSach = vmList.Sum(k => k.SoSach);
        ViewBag.TongCuon = vmList.Sum(k => k.SoCuon);
        ViewBag.KeTrong = vmList.Count(k => k.SoSach == 0);

        await LoadKeSachDropdowns();
        return View(vmList.OrderBy(k => k.Tang).ThenBy(k => k.Phong).ThenBy(k => k.TenKe).ToList());
    }

    // ── TẠO KỆ SÁCH ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoKe(KeSachViewModel model)
    {
        if (!ModelState.IsValid)
        { TempData["error"] = "Thông tin không hợp lệ"; return RedirectToAction(nameof(KeSach)); }

        // Tự động tạo MaKe nếu để trống
        if (string.IsNullOrWhiteSpace(model.MaKe))
        {
            var last = await _db.KeSaches.OrderByDescending(k => k.MaKe).Select(k => k.MaKe).FirstOrDefaultAsync();
            string newMa = "KE001";
            if (!string.IsNullOrEmpty(last) && last.StartsWith("KE") && int.TryParse(last[2..], out int n))
                newMa = "KE" + (n + 1).ToString("D3");
            // Đảm bảo không trùng
            while (await _db.KeSaches.AnyAsync(k => k.MaKe == newMa))
            {
                int num = int.Parse(newMa[2..]) + 1;
                newMa = "KE" + num.ToString("D3");
            }
            model.MaKe = newMa;
        }

        var finalMaKe = model.MaKe!.Trim().ToUpper();
        if (await _db.KeSaches.AnyAsync(k => k.MaKe == finalMaKe))
        { TempData["error"] = "Mã kệ đã tồn tại"; return RedirectToAction(nameof(KeSach)); }

        _db.KeSaches.Add(new KeSach
        {
            MaKe = model.MaKe!.Trim().ToUpper(),
            TenKe = model.TenKe.Trim(),
            ViTri = model.ViTri?.Trim(),
            Tang = model.Tang,
            Phong = model.Phong?.Trim(),
            MoTa = model.MoTa?.Trim(),
            SucChua = model.SucChua,
            MaNXBPhuTrach = string.IsNullOrEmpty(model.MaNXBPhuTrach) ? null : model.MaNXBPhuTrach,
            MaTheLoaiPhuTrach = string.IsNullOrEmpty(model.MaTheLoaiPhuTrach) ? null : model.MaTheLoaiPhuTrach,
            TrangThai = model.TrangThai,
        });
        await _db.SaveChangesAsync();
        TempData["success"] = $"Đã tạo kệ sách {model.TenKe}";
        return RedirectToAction(nameof(KeSach));
    }

    // ── SỬA KỆ SÁCH (GET) ────────────────────────────────────────
    public async Task<IActionResult> SuaKe(string id)
    {
        var ke = await _db.KeSaches.FindAsync(id);
        if (ke == null) return NotFound();
        await LoadKeSachDropdowns();
        return View(new KeSachViewModel
        {
            MaKe = ke.MaKe,
            TenKe = ke.TenKe,
            ViTri = ke.ViTri,
            Tang = ke.Tang,
            Phong = ke.Phong,
            MoTa = ke.MoTa,
            SucChua = ke.SucChua,
            MaNXBPhuTrach = ke.MaNXBPhuTrach,
            MaTheLoaiPhuTrach = ke.MaTheLoaiPhuTrach,
            TrangThai = ke.TrangThai,
        });
    }

    // ── SỬA KỆ SÁCH (POST) ───────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaKe(string id, KeSachViewModel model)
    {
        var ke = await _db.KeSaches.FindAsync(id);
        if (ke == null) return NotFound();

        ke.TenKe = model.TenKe.Trim();
        ke.ViTri = model.ViTri?.Trim();
        ke.Tang = model.Tang;
        ke.Phong = model.Phong?.Trim();
        ke.MoTa = model.MoTa?.Trim();
        ke.SucChua = model.SucChua;
        ke.MaNXBPhuTrach = string.IsNullOrEmpty(model.MaNXBPhuTrach) ? null : model.MaNXBPhuTrach;
        ke.MaTheLoaiPhuTrach = string.IsNullOrEmpty(model.MaTheLoaiPhuTrach) ? null : model.MaTheLoaiPhuTrach;
        ke.TrangThai = model.TrangThai;

        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật kệ sách thành công";
        return RedirectToAction(nameof(KeSach));
    }

    // ── XÓA KỆ SÁCH ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> XoaKe(string id)
    {
        var ke = await _db.KeSaches.Include(k => k.Saches).FirstOrDefaultAsync(k => k.MaKe == id);
        if (ke == null) return NotFound();
        if (ke.Saches.Any(s => s.TrangThai))
        {
            TempData["error"] = "Không thể xóa kệ còn chứa sách đang hoạt động. " +
                "Bạn có thể dùng \"Ngừng hoạt động\" để tạm ẩn kệ mà vẫn giữ nguyên dữ liệu sách trên kệ.";
            return RedirectToAction(nameof(KeSach));
        }

        _db.KeSaches.Remove(ke);
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã xóa kệ sách";
        return RedirectToAction(nameof(KeSach));
    }

    // Đổi trạng thái (Đang dùng ⇄ Ngưng dùng) THAY VÌ xóa hẳn — sách đang xếp trên kệ
    // vẫn được giữ nguyên, chỉ ẩn kệ khỏi danh sách hoạt động.
    public async Task<IActionResult> ToggleTrangThaiKe(string id)
    {
        var ke = await _db.KeSaches.FindAsync(id);
        if (ke == null) return NotFound();
        ke.TrangThai = !ke.TrangThai;
        await _db.SaveChangesAsync();
        TempData["success"] = ke.TrangThai ? "Đã kích hoạt lại kệ sách" : "Đã ngừng hoạt động kệ (giữ nguyên dữ liệu sách trên kệ)";
        return RedirectToAction(nameof(KeSach));
    }

    // ── CHI TIẾT KỆ SÁCH ────────────────────────────────────────
    public async Task<IActionResult> ChiTietKe(string id)
    {
        var ke = await _db.KeSaches
            .Include(k => k.NXBPhuTrach)
            .Include(k => k.TheLoaiPhuTrach)
            .Include(k => k.Saches).ThenInclude(s => s.TheLoai)
            .Include(k => k.Saches).ThenInclude(s => s.NhaXuatBan)
            .Include(k => k.Saches).ThenInclude(s => s.CuonSaches)
            .FirstOrDefaultAsync(k => k.MaKe == id);
        if (ke == null) return NotFound();
        return View(ke);
    }

    // ── NHẬP THÊM CUỐN SÁCH (nhập kho) ─────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhapThem(string maSach, int soLuong)
    {
        var sach = await _db.Saches.Include(s => s.CuonSaches).FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach == null) return NotFound();
        if (soLuong <= 0)
        { TempData["error"] = "Số lượng phải lớn hơn 0"; return RedirectToAction(nameof(DanhSachCuon), new { maSach }); }

        int existing = await _db.CuonSaches.CountAsync(c => c.MaSach == maSach);
        for (int i = 0; i < soLuong; i++)
        {
            int next = existing + i + 1;
            string maCuon = $"{maSach}-{next:D3}";
            while (await _db.CuonSaches.AnyAsync(c => c.MaCuonSach == maCuon))
            {
                next++;
                maCuon = $"{maSach}-{next:D3}";
            }
            _db.CuonSaches.Add(new CuonSach
            {
                MaCuonSach = maCuon,
                MaSach = maSach,
                Barcode = maCuon,
                TrangThai = "Có Sẵn",
                NgayNhap = DateTime.Now
            });
        }

        // Cập nhật SoLuongKho và sync SoLuongKhaDung
        sach.SoLuongKho += soLuong;
        await _db.SaveChangesAsync();

        // Sync SoLuongKhaDung sau khi thêm
        sach = await _db.Saches.Include(s => s.CuonSaches).FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach != null)
        {
            sach.SoLuongKhaDung = sach.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
            await _db.SaveChangesAsync();
        }

        TempData["success"] = $"Đã nhập thêm {soLuong} cuốn. Tổng kho: {sach?.SoLuongKho}";
        return RedirectToAction(nameof(DanhSachCuon), new { maSach });
    }

    // ── DANH SÁCH CUỐN CỦA MỘT ĐẦU SÁCH ────────────────────────────────────
    public async Task<IActionResult> DanhSachCuon(string maSach)
    {
        var sach = await _db.Saches
            .Include(s => s.TheLoai)
            .Include(s => s.CuonSaches)
            .FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach == null) return NotFound();

        // Sync SoLuongKhaDung khi xem
        int coSan = sach.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
        if (sach.SoLuongKhaDung != coSan)
        {
            sach.SoLuongKhaDung = coSan;
            await _db.SaveChangesAsync();
        }

        return View(sach);
    }

    // ── ĐỔI TRẠNG THÁI CUỐN SÁCH ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThaiCuon(string maCuon, string trangThai)
    {
        var cuon = await _db.CuonSaches.Include(c => c.Sach).FirstOrDefaultAsync(c => c.MaCuonSach == maCuon);
        if (cuon == null) return NotFound();
        if (cuon.TrangThai == "Đang Mượn" && trangThai != "Đang Mượn")
        {
            TempData["error"] = "Cuốn đang được mượn, không thể đổi trạng thái";
            return RedirectToAction(nameof(DanhSachCuon), new { maSach = cuon.MaSach });
        }
        cuon.TrangThai = trangThai;
        if (cuon.Sach != null)
        {
            var sach = await _db.Saches.Include(s => s.CuonSaches).FirstOrDefaultAsync(s => s.MaSach == cuon.MaSach);
            if (sach != null)
                sach.SoLuongKhaDung = sach.CuonSaches
                    .Where(c => c.MaCuonSach != maCuon)
                    .Count(c => c.TrangThai == "Có Sẵn")
                    + (trangThai == "Có Sẵn" ? 1 : 0);
        }
        await _db.SaveChangesAsync();
        TempData["success"] = $"Đã đổi trạng thái cuốn {maCuon} → {trangThai}";
        return RedirectToAction(nameof(DanhSachCuon), new { maSach = cuon.MaSach });
    }

    // ── XÓA CUỐN SÁCH (chỉ khi không đang mượn) ────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaCuon(string maCuon)
    {
        var cuon = await _db.CuonSaches.FirstOrDefaultAsync(c => c.MaCuonSach == maCuon);
        if (cuon == null) return NotFound();
        if (cuon.TrangThai == "Đang Mượn")
        { TempData["error"] = "Không thể xóa cuốn đang được mượn"; return RedirectToAction(nameof(DanhSachCuon), new { maSach = cuon.MaSach }); }

        // Cuốn đã từng có LỊCH SỬ mượn trả (dù hiện đã trả) thì không xóa cứng được
        // (ràng buộc khóa ngoại Restrict) — gợi ý đánh dấu Mất/Hỏng thay vì xóa hẳn.
        if (await _db.ChiTietMuonTras.AnyAsync(c => c.MaCuonSach == maCuon))
        {
            TempData["error"] = $"Không thể xóa cuốn {maCuon} vì đã có lịch sử mượn trả trong hệ thống. " +
                "Hãy đánh dấu trạng thái \"Mất\" hoặc \"Hỏng\" thay vì xóa hẳn để vẫn giữ được dữ liệu lịch sử.";
            return RedirectToAction(nameof(DanhSachCuon), new { maSach = cuon.MaSach });
        }

        var maSach = cuon.MaSach;
        _db.CuonSaches.Remove(cuon);

        var sach = await _db.Saches.Include(s => s.CuonSaches).FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach != null)
        {
            sach.SoLuongKho = Math.Max(0, sach.SoLuongKho - 1);
        }
        await _db.SaveChangesAsync();

        if (sach != null)
        {
            sach.SoLuongKhaDung = sach.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
            await _db.SaveChangesAsync();
        }

        TempData["success"] = $"Đã xóa cuốn {maCuon}";
        return RedirectToAction(nameof(DanhSachCuon), new { maSach });
    }

    // ── SYNC TẤT CẢ SoLuongKhaDung (admin tool) ─────────────────────────────
    [HttpPost]
    public async Task<IActionResult> SyncTatCa()
    {
        var sachList = await _db.Saches.Include(s => s.CuonSaches).ToListAsync();
        foreach (var s in sachList)
            s.SoLuongKhaDung = s.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
        await _db.SaveChangesAsync();
        TempData["success"] = $"Đã đồng bộ {sachList.Count} sách";
        return RedirectToAction(nameof(Index));
    }

    // ── HELPER ──────────────────────────────────────────────────
    private async Task LoadKeSachDropdowns()
    {
        ViewBag.DanhSachNXB = new SelectList(await _db.NhaXuatBans.OrderBy(n => n.TenNXB).ToListAsync(), "MaNXB", "TenNXB");
        ViewBag.DanhSachTheLoai = new SelectList(await _db.TheLoais.OrderBy(t => t.TenTheLoai).ToListAsync(), "MaTheLoai", "TenTheLoai");
    }
}
// Note: The closing brace of the class was at end of file - this approach won't work
// Will overwrite the full file instead