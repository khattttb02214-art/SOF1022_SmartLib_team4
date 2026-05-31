using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    [Authorize(Roles = "ADMIN,ThuThu")]
    public class SachController : Controller
    {
        private readonly SmartLibDbContext _context;

        public SachController(SmartLibDbContext context)
        {
            _context = context;
        }

        // 1. GET: Danh sách sách + Tìm kiếm theo Tên Sách (Xóa hoàn toàn lỗi gạch đỏ)
        public async Task<IActionResult> Index(string searchString)
        {
            var sachQuery = _context.Saches
                .Include(s => s.MaTheLoaiNavigation)
                .Include(s => s.MaNxbNavigation)
                .Include(s => s.MaKeNavigation)
                .Include(s => s.MaTacGia)
                .Include(s => s.CuonSaches) // BẮT BUỘC PHẢI INCLUDE ĐỂ ĐẾM
                .Where(s => s.TrangThai == true)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                sachQuery = sachQuery.Where(s =>
                    s.TenSach.Contains(searchString) ||
                    s.MaTacGia.Any(t => t.TenTacGia.Contains(searchString))
                );
            }

            // Lấy dữ liệu 1 lần duy nhất
            var danhSachSach = await sachQuery.ToListAsync();

            // Tính toán số lượng cho từng item
            foreach (var item in danhSachSach)
            {
                item.SoLuongKho = item.CuonSaches.Count();
                item.SoLuongKhaDung = item.CuonSaches.Count(c => c.TrangThai == "Có sẵn");
            }

            ViewBag.CurrentFilter = searchString;

            // Trả về danh sách ĐÃ ĐƯỢC TÍNH TOÁN
            return View(danhSachSach);
        }

        // 2. GET: Giao diện THÊM SÁCH
        [HttpGet]
        // Trong GET Create
        public async Task<IActionResult> Create()
        {
            // ... code cũ (TheLoai, NXB, KeSach) ...
            ViewBag.MaTheLoai = new SelectList(await _context.TheLoais.ToListAsync(), "MaTheLoai", "TenTheLoai");
            ViewBag.MaNxb = new SelectList(await _context.NhaXuatBans.ToListAsync(), "MaNxb", "TenNxb");
            ViewBag.MaKe = new SelectList(await _context.KeSaches.ToListAsync(), "MaKe", "TenKe");

            // MỚI: Thêm danh sách tác giả
            ViewBag.TacGiaList = await _context.TacGia.ToListAsync();
            return View();
        }

        // Trong POST Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sach sach, List<string> selectedTacGiaIds, IFormFile fileAnh) // Thêm tham số fileAnh
        {
            await LoadViewBagData();

            if (ModelState.IsValid)
            {
                try
                {
                    // XỬ LÝ ẢNH MỚI:
                    if (fileAnh != null && fileAnh.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileAnh.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileAnh.CopyToAsync(stream);
                        }
                        sach.AnhBia = fileName; // Lưu tên file vào DB
                    }

                    sach.SoLuongKho = 0;
                    sach.SoLuongKhaDung = 0;
                    sach.NgayTao = DateTime.Now;
                    sach.TrangThai = true;
                    // Tránh lỗi UNIQUE KEY với NULL
                    if (string.IsNullOrEmpty(sach.Isbn))
                        sach.Isbn = null;
                    if (string.IsNullOrEmpty(sach.Barcode))
                        sach.Barcode = null;
                    _context.Add(sach);
                    await _context.SaveChangesAsync();

                    // ... (Phần code Tác giả cũ giữ nguyên)
                    if (selectedTacGiaIds != null && selectedTacGiaIds.Any())
                    {
                        foreach (var tgId in selectedTacGiaIds)
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO Sach_TacGia (MaSach, MaTacGia) VALUES ({0}, {1})",
                                sach.MaSach, tgId);
                        }
                    }
                    return RedirectToAction("NhapKho", "Kho", new { maSach = sach.MaSach });
                }
                catch (Exception ex)
                {
                    var innerMsg = ex.InnerException?.Message ?? "Không có inner exception";
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu sách: " + ex.Message + " | Chi tiết: " + innerMsg);
                }
            }
            return View(sach);
        }

        // Hàm phụ để không bị lặp lại code (giúp code sạch và an toàn)
        private async Task LoadViewBagData()
        {
            // Dùng .ToListAsync() để tránh lỗi null bất ngờ
            var theLoais = await _context.TheLoais.ToListAsync() ?? new List<TheLoai>();
            ViewBag.MaTheLoai = new SelectList(theLoais, "MaTheLoai", "TenTheLoai");

            var nxbs = await _context.NhaXuatBans.ToListAsync() ?? new List<NhaXuatBan>();
            ViewBag.MaNxb = new SelectList(nxbs, "MaNxb", "TenNxb");

            var kes = await _context.KeSaches.ToListAsync() ?? new List<KeSach>();
            ViewBag.MaKe = new SelectList(kes, "MaKe", "TenKe");

            ViewBag.TacGiaList = await _context.TacGia.ToListAsync() ?? new List<TacGium>();
        }
        // 5. GET: Giao diện SỬA SÁCH (Đổ dữ liệu cũ lên Form)
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var sach = await _context.Saches
                    .Include(s => s.MaTacGia)
                    .Include(s => s.CuonSaches)
                    .FirstOrDefaultAsync(s => s.MaSach == id);

                if (sach == null) return NotFound();

                sach.SoLuongKho = sach.CuonSaches.Count();
                sach.SoLuongKhaDung = sach.CuonSaches.Count(c => c.TrangThai == "Có sẵn");

                ViewBag.MaTheLoai = new SelectList(await _context.TheLoais.ToListAsync(), "MaTheLoai", "TenTheLoai", sach.MaTheLoai);
                ViewBag.MaNxb = new SelectList(await _context.NhaXuatBans.ToListAsync(), "MaNxb", "TenNxb", sach.MaNxb);
                ViewBag.MaKe = new SelectList(await _context.KeSaches.ToListAsync(), "MaKe", "TenKe", sach.MaKe);
                ViewBag.TacGiaList = await _context.TacGia.ToListAsync();
                ViewBag.SelectedIds = sach.MaTacGia?.Select(t => t.MaTacGia).ToList() ?? new List<string>();

                return View(sach);
            }
            catch (Exception ex)
            {
                // Hiện lỗi ra màn hình luôn
                return Content("LỖI: " + ex.ToString());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Sach sach, List<string> selectedTacGiaIds, IFormFile? fileAnh)
        {
            // 1. Kiểm tra ID trước khi làm gì khác
            if (id != sach.MaSach) return NotFound();

            // 2. BỎ QUA KIỂM TRA ModelState.IsValid TẠM THỜI ĐỂ TEST
            // Vì fileAnh đang bị báo lỗi Required, ta xử lý logic trong try-catch
            try
            {
                var sachDb = await _context.Saches.FindAsync(id);
                if (sachDb == null) return NotFound();

                // Cập nhật thông tin
                sachDb.TenSach = sach.TenSach;
                sachDb.MaTheLoai = sach.MaTheLoai;
                sachDb.MaNxb = sach.MaNxb;
                sachDb.MaKe = sach.MaKe;
                sachDb.NgonNgu = sach.NgonNgu;
                sachDb.MoTa = sach.MoTa;
                sachDb.NgayCapNhat = DateTime.Now;

                // Xử lý ảnh: CHỈ xử lý nếu người dùng có chọn file
                // Xử lý ảnh: CHỈ xử lý nếu người dùng có chọn file
                if (fileAnh != null && fileAnh.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine("=== BẮT ĐẦU XỬ LÝ ẢNH ===");
                    System.Diagnostics.Debug.WriteLine("Tên file: " + fileAnh.FileName);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(fileAnh.FileName);
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                    System.Diagnostics.Debug.WriteLine("Folder path: " + folderPath);
                    System.Diagnostics.Debug.WriteLine("Folder exists: " + Directory.Exists(folderPath));

                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    using (var stream = new FileStream(Path.Combine(folderPath, fileName), FileMode.Create))
                    {
                        await fileAnh.CopyToAsync(stream);
                    }
                    sachDb.AnhBia = fileName;
                    System.Diagnostics.Debug.WriteLine("=== XỬ LÝ ẢNH XONG ===");
                }

                // Lưu DB
                await _context.SaveChangesAsync();

                // Cập nhật tác giả
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Sach_TacGia WHERE MaSach = {0}", id);
                if (selectedTacGiaIds != null)
                {
                    foreach (var tgId in selectedTacGiaIds)
                    {
                        await _context.Database.ExecuteSqlRawAsync("INSERT INTO Sach_TacGia (MaSach, MaTacGia) VALUES ({0}, {1})", id, tgId);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Ghi lỗi ra Output window của Visual Studio
                System.Diagnostics.Debug.WriteLine("=== LỖI EDIT SÁCH ===");
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                // Hiển thị lỗi lên View thay vì crash
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
            }

            // Nếu có lỗi, nạp lại dữ liệu cho view
            await LoadViewBagData();
            ViewBag.SelectedIds = selectedTacGiaIds ?? new List<string>();
            return View(sach);
        }
        // 4. GET hoặc POST: Xử lý Xóa mềm (Ẩn sách) theo Mã Sách
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sach = await _context.Saches.FirstOrDefaultAsync(m => m.MaSach == id);
            if (sach != null)
            {
                // Thay vì xóa hẳn khỏi DB, ta chuyển trạng thái về false (ẩn đi) để tránh lỗi khóa ngoại
                sach.TrangThai = false;
                sach.NgayCapNhat = DateTime.Now;

                _context.Update(sach);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index)); // Xóa xong tự động load lại trang danh sách
        }
        public async Task CapNhatSoLuong(string maSach)
        {
            var sach = await _context.Saches.Include(s => s.CuonSaches).FirstOrDefaultAsync(s => s.MaSach == maSach);
            if (sach != null)
            {
                sach.SoLuongKho = sach.CuonSaches.Count();
                sach.SoLuongKhaDung = sach.CuonSaches.Count(c => c.TrangThai == "Có sẵn");
                await _context.SaveChangesAsync();
            }
        }
    }
}