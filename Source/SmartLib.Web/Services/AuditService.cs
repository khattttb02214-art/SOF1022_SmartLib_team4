using SmartLib.Web.Data;
using SmartLib.Web.Models;

namespace SmartLib.Web.Services;

public class AuditService
{
    private readonly SmartLibDbContext _context;

    public AuditService(SmartLibDbContext context) => _context = context;

    public async Task LogAsync(string maNV, string hanhDong, string moTa)
    {
        _context.NhatKyHoatDongs.Add(new NhatKyHoatDong {
            MaNV = maNV,
            HanhDong = hanhDong,
            MoTa = moTa,
            ThoiGian = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }
}
