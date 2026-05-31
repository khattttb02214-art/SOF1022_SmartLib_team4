using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    [Authorize(Roles = "ADMIN,ThuThu")]
    public class PhieuMuonController : Controller
    {
        private readonly SmartLibDbContext _context;

        public PhieuMuonController(SmartLibDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dsPhieu = await _context.MuonTras
                .Include(m => m.MaDocGiaNavigation)
                .Include(m => m.ChiTietMuonTras)
                    .ThenInclude(ct => ct.MaCuonSachNavigation)
                        .ThenInclude(cs => cs.MaSachNavigation)
                .OrderByDescending(m => m.NgayMuon)
                .ToListAsync();

            // Đếm số phiếu chờ xác nhận
            ViewBag.SoChoXacNhan = dsPhieu.Count(m => m.TrangThai == "Chờ xác nhận");

            return View(dsPhieu);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.DocGia = _context.DocGia.ToList();
            // Đồng bộ trạng thái "Có sẵn"
            ViewBag.Sach = _context.CuonSaches.Where(c => c.TrangThai == "Có sẵn").ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MuonTra muonTra, List<string> selectedSachIds)
        {
            if (muonTra.NgayHenTra <= DateOnly.FromDateTime(DateTime.Now))
                ModelState.AddModelError("NgayHenTra", "Ngày hẹn trả phải là một ngày trong tương lai!");

            if (selectedSachIds == null || selectedSachIds.Count == 0)
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một cuốn sách.");

            if (ModelState.IsValid)
            {
                muonTra.NgayMuon = DateOnly.FromDateTime(DateTime.Now);
                muonTra.TrangThai = "Chưa Trả";
                _context.MuonTras.Add(muonTra);

                foreach (var maCuon in selectedSachIds)
                {
                    _context.ChiTietMuonTras.Add(new ChiTietMuonTra { MaPhieu = muonTra.MaPhieu, MaCuonSach = maCuon });
                    var cuonSach = await _context.CuonSaches.FindAsync(maCuon);
                    if (cuonSach != null)
                    {
                        cuonSach.TrangThai = "Đang Mượn"; // Đồng bộ
                        var sach = await _context.Saches.FindAsync(cuonSach.MaSach);
                        if (sach != null) sach.SoLuongKhaDung -= 1;
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.DocGia = _context.DocGia.ToList();
            ViewBag.Sach = _context.CuonSaches.Where(s => s.TrangThai == "Có sẵn").ToList();
            return View(muonTra);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var phieu = await _context.MuonTras
                .Include(m => m.ChiTietMuonTras)
                    .ThenInclude(ct => ct.MaCuonSachNavigation)
                        .ThenInclude(cs => cs.MaSachNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieu == id);

            if (phieu == null) return NotFound();

            ViewBag.SachCoTheMuon = await _context.CuonSaches
                .Include(c => c.MaSachNavigation)
                .ToListAsync();

            return View(phieu);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, MuonTra muonTra, List<string> addSachIds)
        {
            var phieuDb = await _context.MuonTras
                .Include(m => m.ChiTietMuonTras)
                .FirstOrDefaultAsync(m => m.MaPhieu == id);

            if (phieuDb == null) return NotFound();

            // 1. Trả sách (Chưa Trả -> Đã Trả)
            if (phieuDb.TrangThai != "Đã Trả" && muonTra.TrangThai == "Đã Trả")
            {
                phieuDb.NgayTraThucTe = DateOnly.FromDateTime(DateTime.Now);
                foreach (var ct in phieuDb.ChiTietMuonTras)
                {
                    var cuonSach = await _context.CuonSaches.FindAsync(ct.MaCuonSach);
                    if (cuonSach != null)
                    {
                        cuonSach.TrangThai = "Có sẵn"; // Đồng bộ
                        var sach = await _context.Saches.FindAsync(cuonSach.MaSach);
                        if (sach != null) sach.SoLuongKhaDung += 1;
                    }
                }
            }
            // 2. Mượn lại (Đã Trả -> Chưa Trả)
            else if (phieuDb.TrangThai == "Đã Trả" && muonTra.TrangThai == "Chưa Trả")
            {
                phieuDb.NgayTraThucTe = null;
                foreach (var ct in phieuDb.ChiTietMuonTras)
                {
                    var cuonSach = await _context.CuonSaches.FindAsync(ct.MaCuonSach);
                    if (cuonSach != null)
                    {
                        cuonSach.TrangThai = "Đang Mượn"; // Đồng bộ
                        var sach = await _context.Saches.FindAsync(cuonSach.MaSach);
                        if (sach != null) sach.SoLuongKhaDung -= 1;
                    }
                }
            }

            // 3. Xử lý thêm sách mới
            if (addSachIds != null)
            {
                foreach (var maCuon in addSachIds)
                {
                    if (!phieuDb.ChiTietMuonTras.Any(ct => ct.MaCuonSach == maCuon))
                    {
                        _context.ChiTietMuonTras.Add(new ChiTietMuonTra { MaPhieu = id, MaCuonSach = maCuon });
                        var cuonSach = await _context.CuonSaches.FindAsync(maCuon);
                        if (cuonSach != null)
                        {
                            cuonSach.TrangThai = "Đang Mượn"; // Đồng bộ
                            var sach = await _context.Saches.FindAsync(cuonSach.MaSach);
                            if (sach != null) sach.SoLuongKhaDung -= 1;
                        }
                    }
                }
            }

            phieuDb.NgayHenTra = muonTra.NgayHenTra;
            phieuDb.GhiChu = muonTra.GhiChu;
            phieuDb.TrangThai = muonTra.TrangThai;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TraSach(string id)
        {
            var phieu = await _context.MuonTras.Include(m => m.ChiTietMuonTras).FirstOrDefaultAsync(m => m.MaPhieu == id);
            if (phieu == null) return NotFound();

            phieu.TrangThai = "Đã Trả";
            phieu.NgayTraThucTe = DateOnly.FromDateTime(DateTime.Now);

            foreach (var ct in phieu.ChiTietMuonTras)
            {
                var cuonSach = await _context.CuonSaches.FindAsync(ct.MaCuonSach);
                if (cuonSach != null)
                {
                    cuonSach.TrangThai = "Có sẵn"; // Đồng bộ
                    var sach = await _context.Saches.FindAsync(cuonSach.MaSach);
                    if (sach != null) sach.SoLuongKhaDung += 1;
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> XacNhan(string id)
        {
            var phieu = await _context.MuonTras
                .Include(m => m.MaDocGiaNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieu == id);

            if (phieu == null) return NotFound();

            // Lấy MaSach từ GhiChu
            var maSach = phieu.GhiChu;

            // Chỉ hiện cuốn sách của đúng loại sách đó
            ViewBag.SachCoSan = await _context.CuonSaches
                .Include(c => c.MaSachNavigation)
                .Where(c => c.TrangThai == "Có sẵn" && c.MaSach == maSach)
                .ToListAsync();

            return View(phieu);
        }

        [HttpPost]
        public async Task<IActionResult> XacNhan(string id, List<string> selectedSachIds)
        {
            var phieu = await _context.MuonTras.FindAsync(id);
            if (phieu == null) return NotFound();

            if (selectedSachIds == null || selectedSachIds.Count == 0)
            {
                TempData["Loi"] = "Vui lòng chọn ít nhất một cuốn sách!";
                return RedirectToAction("XacNhan", new { id });
            }

            // Thêm chi tiết mượn trả
            foreach (var maCuon in selectedSachIds)
            {
                _context.ChiTietMuonTras.Add(new ChiTietMuonTra
                {
                    MaPhieu = id,
                    MaCuonSach = maCuon
                });

                var cuonSach = await _context.CuonSaches.FindAsync(maCuon);
                if (cuonSach != null)
                {
                    cuonSach.TrangThai = "Đang Mượn";
                    var sach = await _context.Saches.FindAsync(cuonSach.MaSach);
                    if (sach != null) sach.SoLuongKhaDung -= 1;
                }
            }

            phieu.TrangThai = "Chưa Trả";
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}