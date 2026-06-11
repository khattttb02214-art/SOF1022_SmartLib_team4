using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly SmartLibDbContext _context;


    public DashboardController(
        SmartLibDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        // =========================
        // THỐNG KÊ TỔNG QUAN
        // =========================

        model.TongSach =
            await _context.Saches.CountAsync();

        model.TongDocGia =
            await _context.DocGias.CountAsync();

        model.TongNhanVien =
            await _context.NhanViens.CountAsync();

        model.TongPhieuMuon =
            await _context.MuonTras.CountAsync();

        model.SachDangMuon =
            await _context.MuonTras
                .CountAsync(x =>
                    x.TrangThai == "Đang Mượn");

        model.SachQuaHan =
            await _context.MuonTras
                .CountAsync(x =>
                    x.TrangThai == "Đang Mượn"
                    && x.NgayHenTra < DateTime.Now);

        model.TongTienPhat =
            await _context.MuonTras
                .SumAsync(x =>
                    (decimal?)x.TienPhat)
                ?? 0;

        // =========================
        // BIỂU ĐỒ 7 NGÀY GẦN NHẤT
        // =========================

        model.Labels =
            new List<string>();

        model.Data =
            new List<int>();

        for (int i = 6; i >= 0; i--)
        {
            var day =
                DateTime.Today.AddDays(-i);

            model.Labels.Add(
                day.ToString("dd/MM"));

            var total =
                await _context.MuonTras
                .CountAsync(x =>
                    x.NgayMuon.Date == day.Date);

            model.Data.Add(total);
        }

        return View(model);
    }


}
