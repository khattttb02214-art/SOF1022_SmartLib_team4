using BCrypt.Net;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class DocGiaController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly IWebHostEnvironment _env;
    public DocGiaController(SmartLibDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    public async Task<IActionResult> Index(string? search)
    {
        var q = _db.DocGias.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(d => d.HoTen.Contains(search) || (d.Email != null && d.Email.Contains(search)));
        ViewBag.Search = search;
        return View(await q.OrderBy(d => d.HoTen).ToListAsync());
    }

    public IActionResult Create() => View(new DocGiaViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocGiaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var maDG = await NextMaDG();
        string? avatar = await SaveAvatar(model.AnhDaiDienFile);

        var reader = new DocGia
        {
            MaDocGia = maDG,
            HoTen = model.HoTen,
            Lop = model.Lop,
            Khoa = model.Khoa,
            Email = model.Email,
            SoDienThoai = model.SoDienThoai,
            DiaChi = model.DiaChi,
            NgaySinh = model.NgaySinh,
            NgayTaoThe = DateTime.Now,
            NgayHetHan = model.NgayHetHan,
            TrangThaiThe = true,
            AnhDaiDien = avatar,
            MaTheTV = model.MaTheTV,
            DaDuyet = true
        };
        _db.DocGias.Add(reader);

        if (model.TaoTaiKhoan && !string.IsNullOrEmpty(model.EmailTaiKhoan))
        {
            if (await _db.NhanViens.AnyAsync(n => n.Email == model.EmailTaiKhoan))
            { ModelState.AddModelError("EmailTaiKhoan", "Email tài khoản đã tồn tại"); return View(model); }

            var maNV = await NextMaNV();
            if (!await _db.ChucVus.AnyAsync(c => c.MaChucVu == "STU"))
                _db.ChucVus.Add(new ChucVu { MaChucVu = "STU", TenChucVu = "Sinh Viên" });

            _db.NhanViens.Add(new NhanVien
            {
                MaNV = maNV,
                HoTen = model.HoTen,
                Email = model.EmailTaiKhoan,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau ?? "smartlib123"),
                MaChucVu = "STU",
                TrangThai = true,
                NgayTao = DateTime.Now,
                MaDocGia = maDG
            });
        }

        await _db.SaveChangesAsync();
        TempData["success"] = "Thêm độc giả thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var d = await _db.DocGias.FindAsync(id);
        if (d == null) return NotFound();
        return View(new DocGiaViewModel
        {
            MaDocGia = d.MaDocGia,
            HoTen = d.HoTen,
            Lop = d.Lop,
            Khoa = d.Khoa,
            Email = d.Email,
            SoDienThoai = d.SoDienThoai,
            DiaChi = d.DiaChi,
            NgaySinh = d.NgaySinh,
            NgayHetHan = d.NgayHetHan,
            MaTheTV = d.MaTheTV
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, DocGiaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var d = await _db.DocGias.FindAsync(id);
        if (d == null) return NotFound();

        d.HoTen = model.HoTen; d.Lop = model.Lop; d.Khoa = model.Khoa;
        d.Email = model.Email; d.SoDienThoai = model.SoDienThoai; d.DiaChi = model.DiaChi;
        d.NgaySinh = model.NgaySinh; d.NgayHetHan = model.NgayHetHan;

        if (model.AnhDaiDienFile != null)
            d.AnhDaiDien = await SaveAvatar(model.AnhDaiDienFile);

        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật độc giả thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ToggleCard(string id)
    {
        var d = await _db.DocGias.FindAsync(id);
        if (d == null) return NotFound();
        d.TrangThaiThe = !d.TrangThaiThe;
        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật trạng thái thẻ thành công";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var d = await _db.DocGias
            .FirstOrDefaultAsync(x => x.MaDocGia == id);

        if (d == null)
            return NotFound();


        // Không cho xóa khi đang mượn
        bool dangMuon = await _db.MuonTras
            .AnyAsync(m =>
                m.MaDocGia == id
                && m.TrangThai == "Đang Mượn");


        if (dangMuon)
        {
            TempData["error"] =
                "Không thể xóa: độc giả đang mượn sách";

            return RedirectToAction(nameof(Index));
        }


        // Xóa dữ liệu liên quan trước

        var muonTras = await _db.MuonTras
            .Where(x => x.MaDocGia == id)
            .ToListAsync();


        foreach (var mt in muonTras)
        {
            var chiTiet = await _db.ChiTietMuonTras
                .Where(x => x.MaPhieu == mt.MaPhieu)
                .ToListAsync();

            _db.ChiTietMuonTras.RemoveRange(chiTiet);

        }


        _db.MuonTras.RemoveRange(muonTras);



        var wishlist = await _db.Wishlists
            .Where(x => x.MaDocGia == id)
            .ToListAsync();

        _db.Wishlists.RemoveRange(wishlist);



        var reservation = await _db.Reservations
            .Where(x => x.MaDocGia == id)
            .ToListAsync();

        _db.Reservations.RemoveRange(reservation);



        var thongBao = await _db.ThongBaos
            .Where(x => x.MaDocGia == id)
            .ToListAsync();

        _db.ThongBaos.RemoveRange(thongBao);



        var theThuVien = await _db.TheThuViens
            .Where(x => x.MaDocGia == id)
            .ToListAsync();

        _db.TheThuViens.RemoveRange(theThuVien);



        // Xóa tài khoản nhân viên nếu có

        var account = await _db.NhanViens
            .FirstOrDefaultAsync(x => x.MaDocGia == id);


        if (account != null)
        {
            _db.NhanViens.Remove(account);
        }



        // cuối cùng xóa độc giả

        _db.DocGias.Remove(d);


        await _db.SaveChangesAsync();



        TempData["success"] =
            "Xóa độc giả thành công";


        return RedirectToAction(nameof(Index));
    }

    // ── IMPORT EXCEL (Độc giả số lượng lớn) ─────────────
    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        { TempData["error"] = "Vui lòng chọn file Excel"; return RedirectToAction(nameof(Index)); }

        int them = 0, loi = 0;
        var errors = new List<string>();
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();

        // Đảm bảo role STU tồn tại
        if (!await _db.ChucVus.AnyAsync(c => c.MaChucVu == "STU"))
            _db.ChucVus.Add(new ChucVu { MaChucVu = "STU", TenChucVu = "Sinh Viên" });

        // Headers: HoTen | Email | SoDienThoai | Lop | Khoa | NgaySinh | MatKhau
        for (int row = 2; row <= (ws.LastRowUsed()?.RowNumber() ?? 1); row++)
        {
            var hoTen = ws.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrEmpty(hoTen)) continue;
            try
            {
                var email = ws.Cell(row, 2).GetString().Trim();
                if (!string.IsNullOrEmpty(email) && await _db.NhanViens.AnyAsync(n => n.Email == email))
                { errors.Add($"Dòng {row}: Email {email} đã tồn tại"); loi++; continue; }

                var maDG = await NextMaDG();
                var maNV = await NextMaNV();
                string matKhau = ws.Cell(row, 7).GetString().Trim();
                if (string.IsNullOrEmpty(matKhau)) matKhau = "smartlib@123";

                var dg = new DocGia
                {
                    MaDocGia = maDG,
                    HoTen = hoTen,
                    Email = email,
                    SoDienThoai = ws.Cell(row, 3).GetString().Trim(),
                    Lop = ws.Cell(row, 4).GetString().Trim(),
                    Khoa = ws.Cell(row, 5).GetString().Trim(),
                    NgaySinh = DateTime.TryParse(ws.Cell(row, 6).GetString().Trim(), out DateTime dt) ? dt : null,
                    NgayTaoThe = DateTime.Now,
                    TrangThaiThe = true,
                    DaDuyet = true
                };
                _db.DocGias.Add(dg);

                if (!string.IsNullOrEmpty(email))
                {
                    _db.NhanViens.Add(new NhanVien
                    {
                        MaNV = maNV,
                        HoTen = hoTen,
                        Email = email,
                        MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhau),
                        MaChucVu = "STU",
                        TrangThai = true,
                        NgayTao = DateTime.Now,
                        MaDocGia = maDG
                    });
                }

                await _db.SaveChangesAsync(); // save each row to get accurate next IDs
                them++;
            }
            catch (Exception ex)
            { errors.Add($"Dòng {row}: {ex.Message}"); loi++; }
        }

        TempData["success"] = $"Import {them} độc giả thành công. Lỗi: {loi}.";
        if (errors.Any()) TempData["error"] = string.Join(" | ", errors.Take(3));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("DocGia");
        string[] h = { "Họ tên (*)", "Email", "SĐT", "Lớp", "Khoa", "Ngày sinh (dd/MM/yyyy)", "Mật khẩu (để trống = smartlib@123)" };
        for (int i = 0; i < h.Length; i++) { ws.Cell(1, i + 1).Value = h[i]; ws.Cell(1, i + 1).Style.Font.Bold = true; ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightCyan; }
        ws.Cell(2, 1).Value = "Nguyễn Thị B"; ws.Cell(2, 2).Value = "b.nguyen@student.edu.vn"; ws.Cell(2, 3).Value = "0912345678";
        ws.Cell(2, 4).Value = "CNTT01"; ws.Cell(2, 5).Value = "Công nghệ thông tin"; ws.Cell(2, 6).Value = "15/03/2004"; ws.Cell(2, 7).Value = "";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_DocGia.xlsx");
    }

    private async Task<string> NextMaDG()
    {
        var last = await _db.DocGias.OrderByDescending(d => d.MaDocGia).Select(d => d.MaDocGia).FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(last) && last.StartsWith("DG") && int.TryParse(last[2..], out int n))
            return "DG" + (n + 1).ToString("D3");
        return "DG001";
    }

    private async Task<string> NextMaNV()
    {
        var last = await _db.NhanViens.OrderByDescending(n => n.MaNV).Select(n => n.MaNV).FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(last) && last.StartsWith("NV") && int.TryParse(last[2..], out int n))
            return "NV" + (n + 1).ToString("D3");
        return "NV001";
    }

    private async Task<string?> SaveAvatar(IFormFile? file)
    {
        if (file == null) return null;
        var name = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var folder = Path.Combine(_env.WebRootPath, "uploads/readers");
        Directory.CreateDirectory(folder);
        await using var fs = new FileStream(Path.Combine(folder, name), FileMode.Create);
        await file.CopyToAsync(fs);
        return name;
    }
}