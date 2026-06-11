using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly SmartLibDbContext _db;
    public WishlistController(SmartLibDbContext db) => _db = db;

    string? MaDocGia => User.FindFirst("MaDocGia")?.Value;

    // ── INDEX: Tổng hợp Yêu thích + Gợi ý + Thông báo ─────────
    public async Task<IActionResult> Index(int? folderId)
    {
        var maDocGia = MaDocGia;
        if (string.IsNullOrEmpty(maDocGia)) return RedirectToAction("Login", "Auth");

        // --- Dữ liệu yêu thích ---
        var folders = await _db.WishlistFolders
            .Where(f => f.MaDocGia == maDocGia)
            .OrderBy(f => f.TenDanhMuc)
            .ToListAsync();

        var wishlistQ = _db.Wishlists
            .Include(w => w.Sach).ThenInclude(s => s!.TheLoai)
            .Include(w => w.Sach).ThenInclude(s => s!.NhaXuatBan)
            .Include(w => w.Sach).ThenInclude(s => s!.SachTacGias).ThenInclude(st => st.TacGia)
            .Include(w => w.Folder)
            .Where(w => w.MaDocGia == maDocGia);

        if (folderId.HasValue)
            wishlistQ = wishlistQ.Where(w => w.FolderId == folderId);

        var danhSachYeuThich = await wishlistQ.OrderByDescending(w => w.NgayThem).ToListAsync();

        // --- Sở thích ---
        var soThich = await _db.WishlistPreferences
            .Where(p => p.MaDocGia == maDocGia)
            .ToListAsync();

        // --- Gợi ý sách dựa trên sở thích ---
        var sachDaYeuThich = danhSachYeuThich.Select(w => w.MaSach).ToHashSet();
        var goiYList = await BuildGoiY(maDocGia, soThich, sachDaYeuThich);

        // --- Thông báo sách mới ---
        var thongBaoMoi = await _db.ThongBaos
            .Include(t => t.Sach)
            .Where(t => t.MaDocGia == maDocGia && t.LoaiThongBao == "SACH_MOI")
            .OrderByDescending(t => t.NgayTao)
            .Take(20)
            .ToListAsync();

        var vm = new WishlistViewModel
        {
            DanhSachYeuThich     = danhSachYeuThich,
            SachGoiY             = goiYList,
            ThongBaoMoi          = thongBaoMoi,
            SoThich              = soThich,
            Folders              = folders,
            FolderIdDangChon     = folderId,
            DanhSachTheLoai      = await _db.TheLoais.OrderBy(t => t.TenTheLoai).ToListAsync(),
            DanhSachTacGia       = await _db.TacGias.OrderBy(t => t.TenTacGia).ToListAsync(),
            DanhSachNXB          = await _db.NhaXuatBans.OrderBy(n => n.TenNXB).ToListAsync(),
        };

        ViewBag.FolderSelectList = new SelectList(folders, "Id", "TenDanhMuc");
        ViewBag.SoThongBaoChuaDoc = thongBaoMoi.Count(t => !t.DaDoc);
        return View(vm);
    }

    // ── API: Số thông báo chưa đọc (dùng bởi layout) ───────────
    [HttpGet]
    public async Task<JsonResult> SoThongBaoChuaDoc()
    {
        var maDocGia = MaDocGia;
        if (string.IsNullOrEmpty(maDocGia)) return Json(0);
        var count = await _db.ThongBaos
            .CountAsync(t => t.MaDocGia == maDocGia && !t.DaDoc);
        return Json(count);
    }

    // ── API: Đánh dấu thông báo đã đọc ────────────────────────
    [HttpPost]
    public async Task<JsonResult> DanhDauDaDoc(int id)
    {
        var tb = await _db.ThongBaos.FindAsync(id);
        if (tb != null && tb.MaDocGia == MaDocGia)
        {
            tb.DaDoc = true;
            await _db.SaveChangesAsync();
        }
        return Json(new { ok = true });
    }

    // ── API: Đánh dấu tất cả đã đọc ────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DocTatCa()
    {
        var maDocGia = MaDocGia;
        if (!string.IsNullOrEmpty(maDocGia))
        {
            var chuaDoc = await _db.ThongBaos
                .Where(t => t.MaDocGia == maDocGia && !t.DaDoc)
                .ToListAsync();
            chuaDoc.ForEach(t => t.DaDoc = true);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // ── THÊM SỞ THÍCH ───────────────────────────────────────────
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemSoThich(string loai, string maRef)
    {
        var maDocGia = MaDocGia;
        if (string.IsNullOrEmpty(maDocGia)) return RedirectToAction("Login", "Auth");

        var exists = await _db.WishlistPreferences
            .AnyAsync(p => p.MaDocGia == maDocGia && p.LoaiSoThich == loai && p.MaRef == maRef);

        if (!exists)
        {
            _db.WishlistPreferences.Add(new WishlistPreference
            {
                MaDocGia    = maDocGia,
                LoaiSoThich = loai,
                MaRef       = maRef,
                NgayTao     = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã thêm vào sở thích";
        }
        else
        {
            TempData["warning"] = "Bạn đã theo dõi sở thích này";
        }
        return RedirectToAction(nameof(Index));
    }

    // ── XÓA SỞ THÍCH ────────────────────────────────────────────
    public async Task<IActionResult> XoaSoThich(int id)
    {
        var pref = await _db.WishlistPreferences.FindAsync(id);
        if (pref != null && pref.MaDocGia == MaDocGia)
        {
            _db.WishlistPreferences.Remove(pref);
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã xóa sở thích";
        }
        return RedirectToAction(nameof(Index));
    }

    // ── ADD / REMOVE WISHLIST ────────────────────────────────────
    public async Task<IActionResult> Add(string sachId, int? folderId)
    {
        var maDocGia = MaDocGia;
        if (string.IsNullOrEmpty(maDocGia)) return RedirectToAction("Login", "Auth");

        if (await _db.Wishlists.AnyAsync(w => w.MaSach == sachId && w.MaDocGia == maDocGia))
        { TempData["warning"] = "Sách đã có trong yêu thích"; return Redirect(Request.Headers["Referer"].ToString() ?? "/"); }

        _db.Wishlists.Add(new Wishlist { MaDocGia = maDocGia, MaSach = sachId, NgayThem = DateTime.Now, FolderId = folderId });
        await _db.SaveChangesAsync();
        TempData["success"] = "Đã thêm vào yêu thích";
        return Redirect(Request.Headers["Referer"].ToString() ?? "/");
    }

    public async Task<IActionResult> Remove(int id)
    {
        var item = await _db.Wishlists.FindAsync(id);
        if (item != null && item.MaDocGia == MaDocGia)
        { _db.Wishlists.Remove(item); await _db.SaveChangesAsync(); TempData["success"] = "Đã xóa khỏi yêu thích"; }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MoveToFolder(int id, int? folderId)
    {
        var item = await _db.Wishlists.FindAsync(id);
        if (item != null && item.MaDocGia == MaDocGia)
        { item.FolderId = folderId; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    // ── FOLDER CRUD ──────────────────────────────────────────────
    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(string TenDanhMuc)
    {
        var maDocGia = MaDocGia;
        if (string.IsNullOrEmpty(maDocGia) || string.IsNullOrWhiteSpace(TenDanhMuc))
            return RedirectToAction(nameof(Index));
        _db.WishlistFolders.Add(new WishlistFolder { TenDanhMuc = TenDanhMuc.Trim(), MaDocGia = maDocGia, NgayTao = DateTime.Now });
        await _db.SaveChangesAsync();
        TempData["success"] = "Tạo danh mục thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DeleteFolder(int id)
    {
        var folder = await _db.WishlistFolders.FindAsync(id);
        if (folder != null && folder.MaDocGia == MaDocGia)
        {
            var items = await _db.Wishlists.Where(w => w.FolderId == id).ToListAsync();
            items.ForEach(i => i.FolderId = null);
            _db.WishlistFolders.Remove(folder);
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã xóa danh mục";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameFolder(int id, string TenDanhMuc)
    {
        var folder = await _db.WishlistFolders.FindAsync(id);
        if (folder != null && folder.MaDocGia == MaDocGia && !string.IsNullOrWhiteSpace(TenDanhMuc))
        { folder.TenDanhMuc = TenDanhMuc.Trim(); await _db.SaveChangesAsync(); TempData["success"] = "Đổi tên thành công"; }
        return RedirectToAction(nameof(Index));
    }

    // ── PRIVATE: Xây dựng danh sách gợi ý ──────────────────────
    private async Task<List<SachGoiY>> BuildGoiY(
        string maDocGia,
        List<WishlistPreference> soThich,
        HashSet<string?> sachDaYeuThich)
    {
        var result = new Dictionary<string, SachGoiY>();

        // Thể loại yêu thích
        var maTheLoais = soThich.Where(p => p.LoaiSoThich == "THELOAI").Select(p => p.MaRef).ToList();
        if (maTheLoais.Any())
        {
            var sachTheLoai = await _db.Saches
                .Include(s => s.TheLoai).Include(s => s.NhaXuatBan)
                .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
                .Where(s => s.TrangThai && maTheLoais.Contains(s.MaTheLoai!) && !sachDaYeuThich.Contains(s.MaSach))
                .OrderByDescending(s => s.NgayTao)
                .Take(12)
                .ToListAsync();

            foreach (var s in sachTheLoai)
                result.TryAdd(s.MaSach, new SachGoiY
                {
                    Sach = s,
                    LyDo = $"Thể loại: {s.TheLoai?.TenTheLoai}",
                    LoaiGoiY = "THELOAI"
                });
        }

        // Tác giả yêu thích
        var maTacGias = soThich.Where(p => p.LoaiSoThich == "TACGIA").Select(p => p.MaRef).ToList();
        if (maTacGias.Any())
        {
            var sachTacGia = await _db.Saches
                .Include(s => s.TheLoai).Include(s => s.NhaXuatBan)
                .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
                .Where(s => s.TrangThai
                    && s.SachTacGias.Any(st => maTacGias.Contains(st.MaTacGia))
                    && !sachDaYeuThich.Contains(s.MaSach))
                .OrderByDescending(s => s.NgayTao)
                .Take(8)
                .ToListAsync();

            foreach (var s in sachTacGia)
            {
                var tenTG = s.SachTacGias
                    .Where(st => maTacGias.Contains(st.MaTacGia))
                    .Select(st => st.TacGia?.TenTacGia)
                    .FirstOrDefault();
                result.TryAdd(s.MaSach, new SachGoiY
                {
                    Sach = s,
                    LyDo = $"Tác giả: {tenTG}",
                    LoaiGoiY = "TACGIA"
                });
            }
        }

        // NXB yêu thích
        var maNXBs = soThich.Where(p => p.LoaiSoThich == "NXB").Select(p => p.MaRef).ToList();
        if (maNXBs.Any())
        {
            var sachNXB = await _db.Saches
                .Include(s => s.TheLoai).Include(s => s.NhaXuatBan)
                .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
                .Where(s => s.TrangThai && maNXBs.Contains(s.MaNXB!) && !sachDaYeuThich.Contains(s.MaSach))
                .OrderByDescending(s => s.NgayTao)
                .Take(8)
                .ToListAsync();

            foreach (var s in sachNXB)
                result.TryAdd(s.MaSach, new SachGoiY
                {
                    Sach = s,
                    LyDo = $"NXB: {s.NhaXuatBan?.TenNXB}",
                    LoaiGoiY = "NXB"
                });
        }

        // Nếu chưa có sở thích → gợi ý sách mới nhất
        if (!soThich.Any())
        {
            var sachMoi = await _db.Saches
                .Include(s => s.TheLoai).Include(s => s.NhaXuatBan)
                .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
                .Where(s => s.TrangThai && !sachDaYeuThich.Contains(s.MaSach))
                .OrderByDescending(s => s.NgayTao)
                .Take(12)
                .ToListAsync();

            foreach (var s in sachMoi)
                result.TryAdd(s.MaSach, new SachGoiY
                {
                    Sach = s,
                    LyDo = "Sách mới nhất",
                    LoaiGoiY = "MOI"
                });
        }

        // Đánh dấu đã yêu thích
        var allWishlist = await _db.Wishlists
            .Where(w => w.MaDocGia == maDocGia)
            .Select(w => w.MaSach)
            .ToHashSetAsync();

        foreach (var g in result.Values)
            g.DaYeuThich = allWishlist.Contains(g.Sach.MaSach);

        return result.Values.OrderBy(_ => Guid.NewGuid()).Take(16).ToList();
    }
}
