using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Attributes;
using SmartLib.Web.Data;
using SmartLib.Web.Interfaces;
using SmartLib.Web.Models;
using SmartLib.Web.Services;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

public class BooksController : Controller
{
    private readonly SmartLibDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly INotificationService _notify;
    private readonly AuditService _auditService;

    public BooksController(SmartLibDbContext db, IWebHostEnvironment env, INotificationService notify, AuditService auditService)
    { _db = db; _env = env; _notify = notify; _auditService = auditService; }

    // ── Helpers ──────────────────────────────────────────
    private async Task LoadDropdowns(BookViewModel? model = null)
    {
        ViewBag.TheLoai = new SelectList(await _db.TheLoais.OrderBy(t => t.TenTheLoai).ToListAsync(), "MaTheLoai", "TenTheLoai", model?.MaTheLoai);
        ViewBag.NXB = new SelectList(await _db.NhaXuatBans.OrderBy(n => n.TenNXB).ToListAsync(), "MaNXB", "TenNXB", model?.MaNXB);
        ViewBag.KeSach = new SelectList(await _db.KeSaches.OrderBy(k => k.TenKe).ToListAsync(), "MaKe", "TenKe", model?.MaKe);
        // SelectList (thay vì List<TacGia> như trước) để dùng asp-items trực tiếp cho
        // ô chọn nhiều (multi-select) — chỗ chọn sẵn (selected) do asp-for tự khớp với
        // Model.SelectedTacGias, không cần truyền selectedValue vào đây.
        ViewBag.TacGias = new SelectList(await _db.TacGias.OrderBy(t => t.TenTacGia).ToListAsync(), "MaTacGia", "TenTacGia");
    }

    // ── Tạo CuonSach records tự động khi nhập sách ───────
    // LƯU Ý: soLuong cuốn được tạo trong CÙNG 1 lượt, CHƯA lưu xuống DB cho tới
    // khi SaveChangesAsync() ở cuối action Create — nên không thể chỉ dựa vào
    // truy vấn CountAsync/AnyAsync (đọc từ DB) để tính mã tiếp theo, vì các cuốn
    // vừa Add() ở vòng lặp trước đó CHƯA nằm trong DB, dễ sinh trùng MaCuonSach
    // (lỗi "already being tracked"). Phải cộng thêm chỉ số vòng lặp (i) và kiểm
    // tra thêm cả ChangeTracker (bản ghi đang chờ lưu) để chắc chắn không trùng.
    private async Task CreateCuonSachRecords(string maSach, int soLuong)
    {
        int existing = await _db.CuonSaches.CountAsync(c => c.MaSach == maSach);
        for (int i = 0; i < soLuong; i++)
        {
            int next = existing + i + 1;
            string maCuon = $"{maSach}-{next:D3}";
            while (await _db.CuonSaches.AnyAsync(c => c.MaCuonSach == maCuon)
                || _db.ChangeTracker.Entries<CuonSach>().Any(e => e.Entity.MaCuonSach == maCuon))
            {
                next++;
                maCuon = $"{maSach}-{next:D3}";
            }
            _db.CuonSaches.Add(new CuonSach
            {
                MaCuonSach = maCuon,
                MaSach = maSach,
                Barcode = maCuon,   // Barcode = maCuon mặc định, admin có thể sửa sau
                TrangThai = "Có Sẵn",
                NgayNhap = DateTime.Now
            });
        }
    }

    // ── Auto-generate MaSach ──────────────────────────────
    private async Task<string> GenerateMaSach()
    {
        var last = await _db.Saches
            .OrderByDescending(s => s.MaSach)
            .Select(s => s.MaSach)
            .FirstOrDefaultAsync();
        int next = 1;
        if (!string.IsNullOrEmpty(last) && last.StartsWith("S") && int.TryParse(last[1..], out int n))
            next = n + 1;
        string ma = "S" + next.ToString("D4");
        while (await _db.Saches.AnyAsync(s => s.MaSach == ma))
        {
            next++;
            ma = "S" + next.ToString("D4");
        }
        return ma;
    }

    // ── Auto-generate MaTheLoai ───────────────────────────
    private async Task<string> GenerateMaTheLoai()
    {
        var last = await _db.TheLoais
            .OrderByDescending(t => t.MaTheLoai)
            .Select(t => t.MaTheLoai)
            .FirstOrDefaultAsync();
        int next = 1;
        if (!string.IsNullOrEmpty(last) && last.StartsWith("TL") && int.TryParse(last[2..], out int n))
            next = n + 1;
        string ma = "TL" + next.ToString("D3");
        while (await _db.TheLoais.AnyAsync(t => t.MaTheLoai == ma))
        {
            next++;
            ma = "TL" + next.ToString("D3");
        }
        return ma;
    }

    // ── INDEX (ADMIN/LIB) ─────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Index(string? search, string? theLoai)
    {
        var q = _db.Saches.Include(s => s.TheLoai).Include(s => s.NhaXuatBan).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(s => s.TenSach.Contains(search) || (s.ISBN != null && s.ISBN.Contains(search)));
        if (!string.IsNullOrEmpty(theLoai))
            q = q.Where(s => s.MaTheLoai == theLoai);
        ViewBag.Search = search;
        ViewBag.TheLoai = theLoai;
        ViewBag.TheLoais = await _db.TheLoais.ToListAsync();
        return View(await q.OrderByDescending(s => s.NgayTao).ToListAsync());
    }

    // ── DETAIL (công khai) ────────────────────────────────
    [AllowAnonymous]
    public async Task<IActionResult> Detail(string id)
    {
        var sach = await _db.Saches
            .Include(s => s.TheLoai)
            .Include(s => s.NhaXuatBan)
            .Include(s => s.KeSach)
                .ThenInclude(k => k!.TheLoaiPhuTrach)   // Fix: load TheLoaiPhuTrach để tránh null ref
            .Include(s => s.KeSach)
                .ThenInclude(k => k!.NXBPhuTrach)
            .Include(s => s.SachTacGias).ThenInclude(st => st.TacGia)
            .Include(s => s.CuonSaches)
            .FirstOrDefaultAsync(s => s.MaSach == id);
        if (sach == null) return NotFound();

        // Tính đúng số đang mượn từ CuonSach
        var soCoSan = sach.CuonSaches.Count(c => c.TrangThai == "Có Sẵn");
        var soDangMuon = sach.CuonSaches.Count(c => c.TrangThai == "Đang Mượn");

        ViewBag.SoDangMuon = soDangMuon;
        ViewBag.SoCoSanThucTe = soCoSan;

        // Đồng bộ SoLuongKhaDung nếu lệch
        if (sach.SoLuongKhaDung != soCoSan)
        {
            sach.SoLuongKhaDung = soCoSan;
            await _db.SaveChangesAsync();
        }

        // Đánh giá sách: chỉ lấy các đánh giá đang ở trạng thái "Hiển thị" để show công khai
        ViewBag.DanhSachDanhGia = await _db.DanhGiaSaches
            .Include(d => d.DocGia)
            .Where(d => d.MaSach == id && d.TrangThai == "Hiển thị")
            .OrderByDescending(d => d.NgayDanhGia)
            .ToListAsync();

        if (User.Identity?.IsAuthenticated == true)
        {
            var maDocGia = User.FindFirst("MaDocGia")?.Value;
            if (!string.IsNullOrEmpty(maDocGia))
            {
                ViewBag.DaYeuThich = await _db.Wishlists.AnyAsync(w => w.MaSach == id && w.MaDocGia == maDocGia);

                // Ràng buộc đánh giá: chỉ độc giả đã từng mượn sách này mới được viết đánh giá
                ViewBag.DaTungMuonSach = await _db.ChiTietMuonTras
                    .AnyAsync(ct => ct.MaSach == id && ct.MuonTra != null && ct.MuonTra.MaDocGia == maDocGia);

                // Đánh giá hiện có của chính người dùng này (nếu có) để hiển thị sẵn lên form sửa
                ViewBag.DanhGiaCuaToi = await _db.DanhGiaSaches
                    .FirstOrDefaultAsync(d => d.MaSach == id && d.MaDocGia == maDocGia);
            }
        }

        return View(sach);
    }

    // ── CREATE ────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        var vm = new BookViewModel
        {
            MaSach = await GenerateMaSach()   // Pre-fill mã tự động
        };
        return View(vm);
    }

    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookViewModel model)
    {
        // Nếu mã trống (không nên xảy ra) thì auto-gen lại
        if (string.IsNullOrWhiteSpace(model.MaSach))
            model.MaSach = await GenerateMaSach();

        // Ảnh bìa bắt buộc khi TẠO MỚI (không đặt [Required] trực tiếp trên
        // ViewModel vì Edit dùng chung model này và không bắt buộc chọn lại ảnh).
        if (model.AnhBiaFile == null)
            ModelState.AddModelError(nameof(model.AnhBiaFile), "Vui lòng chọn ảnh bìa cho sách");

        if (!ModelState.IsValid) { await LoadDropdowns(model); return View(model); }
        if (await _db.Saches.AnyAsync(s => s.MaSach == model.MaSach))
        {
            model.MaSach = await GenerateMaSach();
        }

        // Barcode để trống → mặc định lấy luôn theo Mã sách (giống cách CuonSach
        // cũng mặc định Barcode = MaCuonSach nếu không nhập riêng).
        string barcode = string.IsNullOrWhiteSpace(model.Barcode) ? model.MaSach! : model.Barcode;

        string? img = await SaveImage(model.AnhBiaFile);

        var sach = new Sach
        {
            MaSach = model.MaSach,
            TenSach = model.TenSach,
            ISBN = model.ISBN,
            Barcode = barcode,
            MaTheLoai = model.MaTheLoai,
            MaNXB = model.MaNXB,
            MaKe = model.MaKe,
            NamXuatBan = model.NamXuatBan,
            NgonNgu = model.NgonNgu,
            SoTrang = model.SoTrang,
            MoTa = model.MoTa,
            SoLuongKho = model.SoLuongKho,
            // Lấy đúng theo số người dùng nhập — [NhoHonHoacBang] trên ViewModel đã
            // đảm bảo ModelState chỉ hợp lệ khi SoLuongKhaDung <= SoLuongKho nên
            // không còn lo trường hợp phi lý "khả dụng nhiều hơn tổng kho" nữa (kể cả
            // khi có ai đó bypass JS rồi POST tay lên, IsValid() ở attribute vẫn chặn).
            //
            // LƯU Ý (giới hạn hiện tại): CreateCuonSachRecords() bên dưới vẫn tạo ĐỦ
            // SoLuongKho cuốn và tất cả đều "Có Sẵn" (giống hệt logic cũ). Trong khi đó
            // BorrowController/ReservationController/KhoController/Detail() đều coi
            // CuonSach là "nguồn sự thật" và tự SyncSoLuongKhaDung() = đếm số "Có Sẵn"
            // mỗi khi động tới sách này. Nghĩa là nếu nhập khả dụng THẤP HƠN kho lúc tạo
            // mới, giá trị đó chỉ hiển thị đúng cho tới lần đầu tiên 1 trong các module
            // trên chạy tới sách này — sau đó sẽ tự động bị ghi đè lại = SoLuongKho. Nếu
            // cần giữ đúng vĩnh viễn, cần thêm 1 trạng thái CuonSach mới (VD "Chưa Mở
            // Bán"/"Tạm Khóa") và cho CreateCuonSachRecords gán trạng thái đó cho phần
            // chênh lệch — việc này ảnh hưởng nhiều module nên chưa tự ý thêm ở đây.
            SoLuongKhaDung = model.SoLuongKhaDung,
            AnhBia = img,
            TrangThai = true,
            NgayTao = DateTime.Now
        };
        _db.Saches.Add(sach);

        foreach (var maTG in model.SelectedTacGias)
            _db.SachTacGias.Add(new Sach_TacGia { MaSach = model.MaSach, MaTacGia = maTG });

        await _db.SaveChangesAsync();

        // Tự động tạo CuonSach records theo SoLuongKho
        if (model.SoLuongKho > 0)
        {
            await CreateCuonSachRecords(model.MaSach!, model.SoLuongKho);
            await _db.SaveChangesAsync();
        }

        await _notify.SendNotificationAsync("Có sách mới được thêm: " + model.TenSach);
        TempData["success"] = "Thêm sách thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── API: Tạo thể loại mới nhanh ──────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ThuocChucNang(4)] // Thể loại sách — tách riêng khỏi "Quản lý sách" (cùng Controller=Books)
    public async Task<IActionResult> CreateTheLoaiAjax([FromBody] CreateTheLoaiRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TenTheLoai))
            return Json(new { success = false, message = "Tên thể loại không được trống" });

        // Kiểm tra trùng tên
        if (await _db.TheLoais.AnyAsync(t => t.TenTheLoai == req.TenTheLoai.Trim()))
            return Json(new { success = false, message = "Thể loại này đã tồn tại" });

        var ma = await GenerateMaTheLoai();
        _db.TheLoais.Add(new TheLoai
        {
            MaTheLoai = ma,
            TenTheLoai = req.TenTheLoai.Trim(),
            MoTa = req.MoTa?.Trim()
        });
        await _db.SaveChangesAsync();
        return Json(new { success = true, maTheLoai = ma, tenTheLoai = req.TenTheLoai.Trim() });
    }

    // ── EDIT ──────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Edit(string id)
    {
        var s = await _db.Saches
            .Include(x => x.SachTacGias)
            .FirstOrDefaultAsync(x => x.MaSach == id);
        if (s == null) return NotFound();
        await LoadDropdowns();
        return View(new BookViewModel
        {
            MaSach = s.MaSach,
            TenSach = s.TenSach,
            ISBN = s.ISBN,
            Barcode = s.Barcode,
            MaTheLoai = s.MaTheLoai,
            MaNXB = s.MaNXB,
            MaKe = s.MaKe,
            NamXuatBan = s.NamXuatBan,
            NgonNgu = s.NgonNgu,
            SoTrang = s.SoTrang,
            MoTa = s.MoTa,
            SoLuongKho = s.SoLuongKho,
            SoLuongKhaDung = s.SoLuongKhaDung,
            AnhBia = s.AnhBia,
            SelectedTacGias = s.SachTacGias.Select(st => st.MaTacGia).ToList()
        });
    }

    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, BookViewModel model)
    {
        // Sửa sách KHÔNG bắt buộc chọn lại ảnh bìa (giữ ảnh cũ nếu không chọn ảnh mới).
        ModelState.Remove(nameof(model.AnhBiaFile));

        if (!ModelState.IsValid) { await LoadDropdowns(model); return View(model); }
        var s = await _db.Saches.Include(x => x.SachTacGias).FirstOrDefaultAsync(x => x.MaSach == id);
        if (s == null) return NotFound();

        s.TenSach = model.TenSach;
        s.ISBN = model.ISBN;
        s.Barcode = string.IsNullOrWhiteSpace(model.Barcode) ? s.MaSach : model.Barcode;
        s.MaTheLoai = model.MaTheLoai;
        s.MaNXB = model.MaNXB;
        s.MaKe = model.MaKe;
        s.NamXuatBan = model.NamXuatBan;
        s.NgonNgu = model.NgonNgu;
        s.SoTrang = model.SoTrang;
        s.MoTa = model.MoTa;
        // KHÔNG cho sửa SoLuongKho/SoLuongKhaDung trực tiếp ở đây — 2 số này phải
        // luôn khớp với số CuonSach thực tế trong kho, tự ý gõ tay ở màn Sửa dễ
        // làm lệch dữ liệu (y hệt lỗi "khả dụng > tổng kho" đã gặp trước đó). Muốn
        // thay đổi số lượng, dùng đúng chức năng "Nhập thêm" / xóa cuốn ở màn Kho.
        s.NgayCapNhat = DateTime.Now;

        if (model.AnhBiaFile != null)
            s.AnhBia = await SaveImage(model.AnhBiaFile);

        _db.SachTacGias.RemoveRange(s.SachTacGias);
        foreach (var maTG in model.SelectedTacGias)
            _db.SachTacGias.Add(new Sach_TacGia { MaSach = id, MaTacGia = maTG });

        await _db.SaveChangesAsync();
        TempData["success"] = "Cập nhật sách thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── DELETE ────────────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var s = await _db.Saches.FindAsync(id);
        if (s == null) return NotFound();
        s.TrangThai = !s.TrangThai;
        await _db.SaveChangesAsync();
        TempData["success"] = s.TrangThai ? "Đã kích hoạt lại sách" : "Đã ngừng hoạt động sách (ẩn khỏi kho, giữ nguyên dữ liệu)";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> Delete(string id)
    {
        var s = await _db.Saches.FindAsync(id);
        if (s == null) return NotFound();

        // Sách còn vướng dữ liệu liên quan (đã từng mượn, có cuốn trong kho, có
        // đánh giá, có đặt trước hoặc nằm trong wishlist của độc giả) thì KHÔNG
        // thể xóa cứng — các ràng buộc khóa ngoại này đều là Restrict để bảo toàn
        // lịch sử. Kiểm tra trước và báo lý do cụ thể thay vì để lỗi CSDL văng ra.
        var lyDo = new List<string>();
        if (await _db.CuonSaches.AnyAsync(c => c.MaSach == id)) lyDo.Add("còn cuốn sách trong kho");
        if (await _db.ChiTietMuonTras.AnyAsync(c => c.MaSach == id)) lyDo.Add("đã từng có lượt mượn");
        if (await _db.DanhGiaSaches.AnyAsync(d => d.MaSach == id)) lyDo.Add("đã có đánh giá của độc giả");
        if (await _db.ChiTietDatTruocs.AnyAsync(ct => ct.MaSach == id)) lyDo.Add("đang có đặt trước");
        if (await _db.Wishlists.AnyAsync(w => w.MaSach == id)) lyDo.Add("đang nằm trong wishlist của độc giả");
        if (await _db.Ebooks.AnyAsync(e => e.MaSach == id)) lyDo.Add("đang có file ebook đính kèm");

        if (lyDo.Count > 0)
        {
            TempData["error"] = $"Không thể xóa sách \"{s.TenSach}\" vì {string.Join(", ", lyDo)}. " +
                "Bạn có thể dùng nút \"Ngừng hoạt động\" để ẩn sách khỏi kho mà vẫn giữ nguyên dữ liệu, thay vì xóa hẳn.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrEmpty(s.AnhBia))
        {
            var p = Path.Combine(_env.WebRootPath, "uploads/books", s.AnhBia);
            if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
        }
        _db.Saches.Remove(s);
        await _db.SaveChangesAsync();
        TempData["success"] = "Xóa sách thành công";
        return RedirectToAction(nameof(Index));
    }

    // ── EXPORT EXCEL ──────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public async Task<IActionResult> ExportExcel()
    {
        var books = await _db.Saches.Include(s => s.TheLoai).Include(s => s.NhaXuatBan).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sách");
        string[] headers = { "Mã sách", "Tên sách", "ISBN", "Thể loại", "NXB", "Năm XB", "Ngôn ngữ", "Kho", "Khả dụng" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
        int row = 2;
        foreach (var b in books)
        {
            ws.Cell(row, 1).Value = b.MaSach;
            ws.Cell(row, 2).Value = b.TenSach;
            ws.Cell(row, 3).Value = b.ISBN;
            ws.Cell(row, 4).Value = b.TheLoai?.TenTheLoai;
            ws.Cell(row, 5).Value = b.NhaXuatBan?.TenNXB;
            ws.Cell(row, 6).Value = b.NamXuatBan;
            ws.Cell(row, 7).Value = b.NgonNgu;
            ws.Cell(row, 8).Value = b.SoLuongKho;
            ws.Cell(row, 9).Value = b.SoLuongKhaDung;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DanhSachSach.xlsx");
    }

    // ── IMPORT EXCEL ──────────────────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
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

        var lastSach = await _db.Saches
            .OrderByDescending(s => s.MaSach)
            .Select(s => s.MaSach)
            .FirstOrDefaultAsync();
        int nextNum = 1;
        if (!string.IsNullOrEmpty(lastSach) && lastSach.StartsWith("S") && int.TryParse(lastSach[1..], out int ln))
            nextNum = ln + 1;

        var theLoaiDict = (await _db.TheLoais.ToListAsync()).ToDictionary(t => t.TenTheLoai.Trim().ToLower(), t => t.MaTheLoai);
        var nxbDict = (await _db.NhaXuatBans.ToListAsync()).ToDictionary(n => n.TenNXB.Trim().ToLower(), n => n.MaNXB);
        var tacGiaDict = (await _db.TacGias.ToListAsync()).ToDictionary(t => t.TenTacGia.Trim().ToLower(), t => t.MaTacGia);

        for (int row = 2; row <= (ws.LastRowUsed()?.RowNumber() ?? 1); row++)
        {
            try
            {
                var tenSach = ws.Cell(row, 1).GetString().Trim();
                if (string.IsNullOrEmpty(tenSach)) continue;

                var isbn = ws.Cell(row, 2).GetString().Trim();
                var tenTL = ws.Cell(row, 3).GetString().Trim().ToLower();
                var tenNXB = ws.Cell(row, 4).GetString().Trim().ToLower();
                var tenTG = ws.Cell(row, 5).GetString().Trim().ToLower();
                var namXB = ws.Cell(row, 6).GetString().Trim();
                var ngonNgu = ws.Cell(row, 7).GetString().Trim();
                var soTrang = ws.Cell(row, 8).GetString().Trim();
                var soLuong = ws.Cell(row, 9).GetString().Trim();

                string maSach = "S" + nextNum.ToString("D4");
                while (await _db.Saches.AnyAsync(s => s.MaSach == maSach))
                {
                    nextNum++;
                    maSach = "S" + nextNum.ToString("D4");
                }

                string? maTL = theLoaiDict.TryGetValue(tenTL, out var tl) ? tl : null;
                string? maNXB = nxbDict.TryGetValue(tenNXB, out var nxb) ? nxb : null;
                string? maTG = tacGiaDict.TryGetValue(tenTG, out var tg) ? tg : null;

                var sach = new Sach
                {
                    MaSach = maSach,
                    TenSach = tenSach,
                    ISBN = isbn,
                    MaTheLoai = maTL,
                    MaNXB = maNXB,
                    NamXuatBan = int.TryParse(namXB, out int ny) ? ny : null,
                    NgonNgu = string.IsNullOrEmpty(ngonNgu) ? "Tiếng Việt" : ngonNgu,
                    SoTrang = int.TryParse(soTrang, out int sp) ? sp : null,
                    SoLuongKho = int.TryParse(soLuong, out int sl) ? sl : 0,
                    SoLuongKhaDung = int.TryParse(soLuong, out int sl2) ? sl2 : 0,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };
                _db.Saches.Add(sach);

                if (!string.IsNullOrEmpty(maTG))
                    _db.SachTacGias.Add(new Sach_TacGia { MaSach = maSach, MaTacGia = maTG });

                // Tự tạo CuonSach records giống hệt luồng Create đơn lẻ — nếu thiếu
                // bước này, sách import hàng loạt sẽ không có CuonSach nào, khiến
                // BorrowController/ReservationController/KhoController (coi CuonSach
                // là "nguồn sự thật") không mượn/đặt trước được sách vừa import.
                // Không cần SaveChanges riêng ở đây: EF tự biết insert Sach trước rồi
                // mới tới CuonSach theo đúng thứ tự khóa ngoại khi SaveChangesAsync()
                // gộp chung 1 lần ở cuối vòng lặp (nhanh hơn nhiều so với lưu từng dòng).
                if (sach.SoLuongKho > 0)
                    await CreateCuonSachRecords(maSach, sach.SoLuongKho);

                nextNum++;
                them++;
            }
            catch (Exception ex)
            {
                loi++;
                errors.Add($"Dòng {row}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();
        TempData["success"] = $"Import thành công: {them} sách. Lỗi: {loi}.";
        if (errors.Any()) TempData["error"] = string.Join("; ", errors.Take(5));
        return RedirectToAction(nameof(Index));
    }

    // ── NHẬP ẢNH BÌA HÀNG LOẠT ────────────────────────────
    // Chọn nhiều file ảnh cùng lúc, mỗi file được đặt tên theo ĐÚNG Mã sách
    // (VD "S0001.jpg") hoặc ISBN (VD "9786041234567.png") của sách cần gán —
    // hệ thống tự dò khớp theo tên file (không tính phần đuôi) rồi cập nhật
    // AnhBia cho đúng sách đó, không cần vào Sửa từng sách một để chọn ảnh.
    [Authorize(Roles = "ADMIN,LIB")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(200_000_000)] // Nới giới hạn dung lượng request — mặc định quá nhỏ cho việc chọn hàng loạt ảnh cùng lúc
    public async Task<IActionResult> BulkAnhBiaUpload(List<IFormFile> files)
    {
        if (files == null || !files.Any())
        {
            TempData["error"] = "Vui lòng chọn ít nhất 1 ảnh bìa.";
            return RedirectToAction(nameof(Index));
        }

        string[] duoiChoPhep = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var folder = Path.Combine(_env.WebRootPath, "uploads/books");
        Directory.CreateDirectory(folder);

        var vm = new BulkAnhBiaKetQuaViewModel();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!duoiChoPhep.Contains(ext))
            {
                vm.KhongKhop.Add($"{file.FileName} (định dạng ảnh không hỗ trợ)");
                continue;
            }

            // Khóa khớp = tên file KHÔNG kèm đuôi, VD "S0001.jpg" → khóa "S0001"
            var key = Path.GetFileNameWithoutExtension(file.FileName).Trim();
            if (string.IsNullOrEmpty(key)) { vm.KhongKhop.Add(file.FileName); continue; }

            var sach = await _db.Saches.FirstOrDefaultAsync(s => s.MaSach == key);
            string khopTheo = "Mã sách";
            if (sach == null)
            {
                sach = await _db.Saches.FirstOrDefaultAsync(s => s.ISBN == key);
                khopTheo = "ISBN";
            }

            if (sach == null)
            {
                vm.KhongKhop.Add(file.FileName);
                continue;
            }

            // Xóa ảnh cũ (nếu còn tồn tại trên đĩa) trước khi lưu ảnh mới — tránh rác
            // tích tụ trong wwwroot/uploads/books qua nhiều lần nhập lại.
            if (!string.IsNullOrEmpty(sach.AnhBia))
            {
                var oldPath = Path.Combine(folder, sach.AnhBia);
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); }
                    catch { /* không chặn import nếu lỡ không xóa được file cũ */ }
                }
            }

            var newName = Guid.NewGuid() + ext;
            await using (var fs = new FileStream(Path.Combine(folder, newName), FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }
            sach.AnhBia = newName;

            vm.DaKhop.Add(new BulkAnhBiaMatchedItem
            {
                TenFile = file.FileName,
                MaSach = sach.MaSach,
                TenSach = sach.TenSach,
                KhopTheo = khopTheo
            });
        }

        await _db.SaveChangesAsync();

        var maNV = User.FindFirst("MaNV")?.Value;
        if (!string.IsNullOrEmpty(maNV))
        {
            await _auditService.LogAsync(maNV, "Nhập ảnh bìa hàng loạt",
                $"Cập nhật {vm.DaKhop.Count} ảnh bìa, {vm.KhongKhop.Count} file không khớp sách nào.");
        }

        return View("BulkAnhBiaKetQua", vm);
    }

    // ── DOWNLOAD TEMPLATE EXCEL ───────────────────────────
    [Authorize(Roles = "ADMIN,LIB")]
    public IActionResult DownloadTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sách");
        string[] headers = { "Tên sách (*)", "ISBN", "Thể loại", "Nhà xuất bản", "Tác giả", "Năm XB", "Ngôn ngữ", "Số trang", "Số lượng" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        ws.Cell(2, 1).Value = "Lập trình C# cơ bản";
        ws.Cell(2, 2).Value = "978-604-xxxxxx";
        ws.Cell(2, 3).Value = "Công nghệ thông tin";
        ws.Cell(2, 4).Value = "NXB Giáo Dục";
        ws.Cell(2, 5).Value = "Nguyễn Văn A";
        ws.Cell(2, 6).Value = "2023";
        ws.Cell(2, 7).Value = "Tiếng Việt";
        ws.Cell(2, 8).Value = "350";
        ws.Cell(2, 9).Value = "5";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Sach.xlsx");
    }

    // ── HELPER: Lưu ảnh bìa ──────────────────────────────
    private async Task<string?> SaveImage(IFormFile? file)
    {
        if (file == null) return null;
        var name = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var folder = Path.Combine(_env.WebRootPath, "uploads/books");
        Directory.CreateDirectory(folder);
        await using var fs = new FileStream(Path.Combine(folder, name), FileMode.Create);
        await file.CopyToAsync(fs);
        return name;
    }
}

// DTO cho API tạo thể loại
public class CreateTheLoaiRequest
{
    public string TenTheLoai { get; set; } = "";
    public string? MoTa { get; set; }
}