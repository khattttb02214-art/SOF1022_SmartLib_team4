using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLib.Web.Data;
using SmartLib.Web.Models;

namespace SmartLib.Web.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly SmartLibDbContext _context;

    public ReviewController(
        SmartLibDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        DanhGiaSach model)
    {
        model.NgayDanhGia =
            DateTime.Now;

        _context.DanhGiaSaches.Add(model);

        await _context.SaveChangesAsync();

        TempData["success"] =
            "Đánh giá thành công";

        return RedirectToAction(
            "Index",
            "Books");
    }
}