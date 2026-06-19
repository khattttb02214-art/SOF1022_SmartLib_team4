using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;

namespace SmartLib.Web.Controllers;

[Authorize(Roles = "ADMIN,LIB")]
public class AuditController : Controller
{
    private readonly SmartLibDbContext _context;

    public AuditController(SmartLibDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? maNV, string? hanhDong, DateTime? tuNgay, DateTime? denNgay)
    {
        var q = _context.NhatKyHoatDongs
            .Include(x => x.NhanVien)
            .AsQueryable();

        if (!string.IsNullOrEmpty(maNV))
            q = q.Where(x => x.MaNV == maNV || (x.NhanVien != null && x.NhanVien.HoTen.Contains(maNV)));

        if (!string.IsNullOrEmpty(hanhDong))
            q = q.Where(x => x.HanhDong != null && x.HanhDong.Contains(hanhDong));

        if (tuNgay.HasValue)
            q = q.Where(x => x.ThoiGian >= tuNgay.Value);

        if (denNgay.HasValue)
            q = q.Where(x => x.ThoiGian <= denNgay.Value.AddDays(1));

        ViewBag.MaNV     = maNV;
        ViewBag.HanhDong = hanhDong;
        ViewBag.TuNgay   = tuNgay?.ToString("yyyy-MM-dd");
        ViewBag.DenNgay  = denNgay?.ToString("yyyy-MM-dd");

        var logs = await q.OrderByDescending(x => x.ThoiGian).Take(1000).ToListAsync();
        return View(logs);
    }
}
