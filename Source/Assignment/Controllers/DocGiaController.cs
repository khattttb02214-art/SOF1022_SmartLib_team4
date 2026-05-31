using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    [Authorize(Roles = "ADMIN,ThuThu")]
    public class DocGiaController : Controller
    {
        private readonly SmartLibDbContext _context;
        public DocGiaController(SmartLibDbContext context) => _context = context;

        // 1. DANH SÁCH & TÌM KIẾM
        public async Task<IActionResult> Index(string searchString)
        {
            // 1. Khởi tạo query từ bảng DocGia
            var list = _context.DocGia.AsQueryable();

            // 2. Nếu có từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                // Dùng .ToLower() để so sánh không phân biệt hoa thường
                // Dùng .Contains() để tìm kiếm gần đúng
                string search = searchString.ToLower();
                list = list.Where(d => d.HoTen.ToLower().Contains(search) ||
                                       d.MaDocGia.ToLower().Contains(search));
            }

            // 3. Nếu bạn muốn khi tìm kiếm nó hiện cả người "Bị khóa", 
            // thì để nguyên như trên.
            // Nếu bạn muốn tìm kiếm mà VẪN LỌC ra những người bị khóa, hãy thêm điều kiện:
            // list = list.Where(d => d.TrangThaiThe == true); 

            return View(await list.OrderBy(d => d.HoTen).ToListAsync());
        }

        // 2. CHI TIẾT
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();
            var docGia = await _context.DocGia.FirstOrDefaultAsync(d => d.MaDocGia == id);
            return docGia == null ? NotFound() : View(docGia);
        }

        // 3. THÊM MỚI
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocGium docGia)
        {
            if (ModelState.IsValid)
            {
                docGia.NgayTaoThe = DateOnly.FromDateTime(DateTime.Now);
                // Tự động gán hạn thẻ là 1 năm kể từ ngày tạo
                docGia.NgayHetHan = DateOnly.FromDateTime(DateTime.Now.AddYears(1));
                docGia.TrangThaiThe = true;

                _context.Add(docGia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(docGia);
        }

        // 4. SỬA
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var docGia = await _context.DocGia.FindAsync(id);
            return docGia == null ? NotFound() : View(docGia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, DocGium docGia, string? matKhauMoi)
        {
            ModelState.Remove("MatKhau");
            if (id != docGia.MaDocGia) return NotFound();

            var d = await _context.DocGia.FindAsync(id);
            if (d == null) return NotFound();

            // Cập nhật thông tin cơ bản
            d.HoTen = docGia.HoTen;
            d.Lop = docGia.Lop;
            d.Khoa = docGia.Khoa;
            d.Email = docGia.Email;
            d.SoDienThoai = docGia.SoDienThoai;
            d.DiaChi = docGia.DiaChi;
            d.NgaySinh = docGia.NgaySinh;

            // Cập nhật thông tin thẻ
            d.NgayHetHan = docGia.NgayHetHan; // Thêm dòng này
            d.TrangThaiThe = docGia.TrangThaiThe; // Thêm dòng này

            // Cập nhật mật khẩu
            if (!string.IsNullOrEmpty(matKhauMoi))
            {
                d.MatKhau = matKhauMoi;
            }

            try
            {
                _context.Update(d);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException) { /* ... */ }

            return RedirectToAction(nameof(Index));
        }

        // 5. XÓA/KHÓA THẺ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var docGia = await _context.DocGia.FindAsync(id);
            if (docGia != null)
            {
                docGia.TrangThaiThe = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoKhoa(string id)
        {
            var docGia = await _context.DocGia.FindAsync(id);
            if (docGia != null)
            {
                docGia.TrangThaiThe = true; // Chuyển sang hoạt động
                _context.Update(docGia);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        // GET: Hiện form để chọn ngày
        public async Task<IActionResult> GiaHan(string id)
        {
            var docGia = await _context.DocGia.FindAsync(id);
            if (docGia == null) return NotFound();

            // Trả về view kèm thông tin độc giả để biết đang gia hạn cho ai
            return View(docGia);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiaHan(string id, DateOnly ngayMoi)
        {
            var docGia = await _context.DocGia.FindAsync(id);
            if (docGia == null) return NotFound();

            // 1. Kiểm tra ngày gia hạn phải lớn hơn ngày hiện tại
            if (ngayMoi <= DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("", "Lỗi: Ngày gia hạn phải là ngày trong tương lai!");
                return View(docGia); // Lỗi sẽ hiện ở ModelOnly trong View
            }

            // 2. Kiểm tra ngày mới phải lớn hơn ngày cũ (nếu đã có ngày hết hạn)
            if (docGia.NgayHetHan.HasValue && ngayMoi <= docGia.NgayHetHan.Value)
            {
                ModelState.AddModelError("", "Lỗi: Ngày gia hạn mới phải sau ngày hết hạn cũ!");
                return View(docGia);
            }

            // 3. Kiểm tra xem độc giả có còn sách chưa trả không (Logic mở rộng)
            bool coSachChuaTra = await _context.MuonTras
                .AnyAsync(m => m.MaDocGia == id && m.TrangThai == "Chưa Trả");

            if (coSachChuaTra)
            {
                ModelState.AddModelError("", "Lỗi: Độc giả này còn sách chưa trả, vui lòng thu hồi sách trước khi gia hạn!");
                return View(docGia);
            }

            // Nếu không lỗi thì lưu
            docGia.NgayHetHan = ngayMoi;
            docGia.TrangThaiThe = true;
            _context.Update(docGia);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}