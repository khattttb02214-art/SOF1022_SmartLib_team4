using Assignment.Models;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "ADMIN,ThuThu")]
public class KhoController : Controller
{
    private readonly SmartLibDbContext _context;

    public KhoController(SmartLibDbContext context)
    {
        _context = context;
    }

    // 1. Danh sách kho: Gom nhóm theo sách
    public async Task<IActionResult> Index()
    {
        var kho = await _context.Saches
            .Include(s => s.CuonSaches)
            .ToListAsync();
        return View(kho);
    }

    // 2. Nhập kho (Thêm mới một cuốn sách cụ thể)
    public IActionResult NhapKho(string maSach)
    {
        ViewBag.MaSach = maSach;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> NhapKho(CuonSach cuonSach)
    {
        ModelState.Remove("MaCuonSach");

        if (ModelState.IsValid)
        {
            // 1. Kiểm tra xem Barcode đã tồn tại trong DB chưa
            bool isBarcodeExists = await _context.CuonSaches.AnyAsync(c => c.Barcode == cuonSach.Barcode);

            if (isBarcodeExists)
            {
                // Thêm lỗi vào ModelState để nó hiện đỏ trên View
                ModelState.AddModelError("Barcode", "Mã vạch này đã tồn tại trong kho!");
                return View(cuonSach);
            }
            // 1. Lấy danh sách mã hiện có an toàn
            var existingCodes = await _context.CuonSaches
                .Where(c => c.MaSach == cuonSach.MaSach)
                .Select(c => c.MaCuonSach)
                .ToListAsync();

            // 2. Tìm số thứ tự tiếp theo
            int nextIndex = 1;
            if (existingCodes.Any())
            {
                int max = 0;
                foreach (var code in existingCodes)
                {
                    // Tách mã CS1-MS003 lấy ra số 1
                    if (!string.IsNullOrEmpty(code) && code.Contains("-"))
                    {
                        var parts = code.Split('-');
                        var numberPart = parts[0].Replace("CS", "");
                        if (int.TryParse(numberPart, out int n))
                        {
                            if (n > max) max = n;
                        }
                    }
                }
                nextIndex = max + 1;
            }

            // 3. Gán mã và lưu
            cuonSach.MaCuonSach = $"CS{nextIndex}-{cuonSach.MaSach}";
            cuonSach.NgayNhap = DateOnly.FromDateTime(DateTime.Now);

            _context.CuonSaches.Add(cuonSach);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        return View(cuonSach);
    }
    public async Task<IActionResult> DongBoKho()
    {
        // 1. Lấy tất cả sách kèm theo danh sách CuonSachs tương ứng
        var tatCaSach = await _context.Saches.Include(s => s.CuonSaches).ToListAsync();

        foreach (var s in tatCaSach)
        {
            // 2. Cập nhật số lượng dựa trên dữ liệu thực tế trong bảng CuonSachs
            s.SoLuongKho = s.CuonSaches.Count();
            s.SoLuongKhaDung = s.CuonSaches.Count(c => c.TrangThai == "Có sẵn");
        }

        // 3. Lưu thay đổi vào DB
        await _context.SaveChangesAsync();

        return RedirectToAction("Index"); // Quay lại trang Index
    }
    [HttpGet]
    public async Task<IActionResult> GetNextBarcode(string maSach)
    {
        // Lấy tất cả barcode dạng BC1, BC2, BC3...
        var allBarcodes = await _context.CuonSaches
            .Select(c => c.Barcode)
            .ToListAsync();

        int max = 0;
        foreach (var bc in allBarcodes)
        {
            if (!string.IsNullOrEmpty(bc) && bc.StartsWith("BC"))
            {
                if (int.TryParse(bc.Substring(2), out int n))
                {
                    if (n > max) max = n;
                }
            }
        }

        return Json(new { barcode = $"BC{max + 1}" });
    }
}