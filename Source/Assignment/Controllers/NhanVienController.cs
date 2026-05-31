using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "ADMIN")]
public class NhanVienController : Controller
{
    private readonly SmartLibDbContext _context;

    public NhanVienController(SmartLibDbContext context)
    {
        _context = context;
    }

    // GET: NHANVIENS
    public async Task<IActionResult> Index(string searchString)
    {
        var nhanViens = _context.NhanViens.Include(n => n.MaChucVuNavigation).AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            nhanViens = nhanViens.Where(n => n.HoTen.Contains(searchString) || n.Email.Contains(searchString));
        }

        ViewBag.CurrentFilter = searchString;
        return View(await nhanViens.ToListAsync());
    }

    // GET: NHANVIENS/Details/5
    public async Task<IActionResult> Details(string? manv)
    {
        if (manv == null) return NotFound();

        var nhanvien = await _context.NhanViens
            .Include(n => n.MaChucVuNavigation)
            .FirstOrDefaultAsync(m => m.MaNv == manv);

        if (nhanvien == null) return NotFound();

        return View(nhanvien);
    }

    // GET: NHANVIENS/Create
    public IActionResult Create()
    {
        // Danh sách chức vụ
        var chucVuList = new List<SelectListItem>
    {
        new SelectListItem { Value = "ADMIN", Text = "Quản trị viên" },
        new SelectListItem { Value = "THUTHU", Text = "Thủ thư" }
    };
        ViewBag.MaChucVu = new SelectList(chucVuList, "Value", "Text");

        // Danh sách trạng thái
        var trangThaiList = new List<SelectListItem>
    {
        new SelectListItem { Value = "true", Text = "Đang hoạt động" },
        new SelectListItem { Value = "false", Text = "Khóa" }
    };
        ViewBag.TrangThaiList = new SelectList(trangThaiList, "Value", "Text");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MaNv,HoTen,Email,SoDienThoai,DiaChi,MatKhau,MaChucVu,TrangThai")] NhanVien nhanvien)
    {
        // CHỐT CHẶN: Nếu tạo mới mà mật khẩu trống, thêm lỗi thủ công
        if (string.IsNullOrWhiteSpace(nhanvien.MatKhau))
        {
            ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống khi tạo mới!");
        }
        if (ModelState.IsValid)
        {
            nhanvien.NgayTao = DateTime.Now; // Tự động set ngày tạo
            _context.Add(nhanvien);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.MaChucVu = new SelectList(_context.ChucVus, "MaChucVu", "TenChucVu", nhanvien.MaChucVu);
        return View(nhanvien);
    }

    // GET: NHANVIENS/Edit/5
    // GET Edit
    // GET Edit
    public async Task<IActionResult> Edit(string? manv)
    {
        if (manv == null) return NotFound();
        var nhanvien = await _context.NhanViens.FindAsync(manv);
        if (nhanvien == null) return NotFound();

        SetViewBagForNhanVien(nhanvien.MaChucVu, nhanvien.TrangThai);
        return View(nhanvien);
    }

    // POST Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    // Đưa MatKhau vào trong [Bind]
    public async Task<IActionResult> Edit(string? manv, [Bind("MaNv,HoTen,Email,SoDienThoai,DiaChi,MatKhau,MaChucVu,TrangThai")] NhanVien nhanvien)
    {
        if (manv != nhanvien.MaNv) return NotFound();

        var oldNhanVien = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(n => n.MaNv == manv);
        if (oldNhanVien == null) return NotFound();

        // Nếu người dùng không nhập mật khẩu mới, dùng lại mật khẩu cũ
        if (string.IsNullOrWhiteSpace(nhanvien.MatKhau))
        {
            nhanvien.MatKhau = oldNhanVien.MatKhau;
        }

        // Tương tự cho Email (nếu bạn muốn cho phép để trống lúc Edit mà vẫn giữ cũ)
        if (string.IsNullOrWhiteSpace(nhanvien.Email))
        {
            nhanvien.Email = oldNhanVien.Email;
        }

        // BỎ QUA lỗi validation cho hai trường này
        ModelState.Remove("MatKhau");
        ModelState.Remove("Email");

        if (ModelState.IsValid)
        {
            try
            {
                nhanvien.NgayCapNhat = DateTime.Now;
                _context.Update(nhanvien);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException) { throw; }
        }

        SetViewBagForNhanVien(nhanvien.MaChucVu, nhanvien.TrangThai);
        return View(nhanvien);
    }

    // GET: NHANVIENS/Delete/5
    public async Task<IActionResult> Delete(string? manv)
    {
        if (manv == null) return NotFound();

        var nhanvien = await _context.NhanViens
            .Include(n => n.MaChucVuNavigation)
            .FirstOrDefaultAsync(m => m.MaNv == manv);

        if (nhanvien == null) return NotFound();
        return View(nhanvien);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? manv)
    {
        var nhanvien = await _context.NhanViens.FindAsync(manv);
        if (nhanvien != null) _context.NhanViens.Remove(nhanvien);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool NhanVienExists(string? manv)
    {
        return _context.NhanViens.Any(e => e.MaNv == manv);
    }
    // Thêm hàm này vào dưới cùng của class
    private void SetViewBagForNhanVien(string? selectedChucVu = null, bool? selectedTrangThai = null)
    {
        var listChucVu = new List<SelectListItem>
    {
        new SelectListItem { Value = "ADMIN", Text = "Quản trị viên" },
        new SelectListItem { Value = "THUTHU", Text = "Thủ thư" }
    };
        ViewBag.MaChucVu = new SelectList(listChucVu, "Value", "Text", selectedChucVu);

        var listTrangThai = new List<SelectListItem>
    {
        new SelectListItem { Value = "true", Text = "Đang hoạt động" },
        new SelectListItem { Value = "false", Text = "Khóa" }
    };
        ViewBag.TrangThaiList = new SelectList(listTrangThai, "Value", "Text", selectedTrangThai?.ToString().ToLower());
    }
}