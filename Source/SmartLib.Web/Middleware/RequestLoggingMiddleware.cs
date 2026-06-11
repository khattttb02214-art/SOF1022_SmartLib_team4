using SmartLib.Web.Data;
using SmartLib.Web.Models;
using System.Security.Claims;

namespace SmartLib.Web.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";

        IFormCollection? form = null;

        if (context.Request.HasFormContentType)
        {
            form = await context.Request.ReadFormAsync();
        }

        await _next(context);

        if (method == "POST" && context.Response.StatusCode is 200 or 302)
        {
            var user = context.User;
            var maNV = user.FindFirst("MaNV")?.Value;

            if (string.IsNullOrEmpty(maNV))
                return;

            var (hanhDong, moTa) = PhanLoai(path, form);

            if (hanhDong == null)
                return;

            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SmartLibDbContext>();

            db.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                MaNV = maNV,
                HanhDong = hanhDong,
                MoTa = moTa,
                ThoiGian = DateTime.Now
            });

            await db.SaveChangesAsync();
        }
    }

    private static (string? hanhDong, string moTa) PhanLoai(string path, IFormCollection? form)
    {
        var p = path.ToLower();

        if (p.Contains("/auth/login"))
            return ("Đăng nhập", "Đăng nhập hệ thống");

        if (p.Contains("/auth/logout"))
            return ("Đăng xuất", "Đăng xuất hệ thống");

        // Books
        if (p.Contains("/books/create"))
            return ("Thêm sách", $"Thêm sách mới: {form?["TenSach"].ToString()}");

        if (p.Contains("/books/edit"))
            return ("Sửa sách", $"Cập nhật sách: {form?["TenSach"].ToString()}");

        if (p.Contains("/books/delete"))
            return ("Xóa sách", "Xóa sách");

        if (p.Contains("/books/importexcel"))
            return ("Import sách", "Import sách từ Excel");

        // Borrow
        if (p.Contains("/borrow/create"))
            return ("Lập phiếu mượn", "Tạo phiếu mượn mới");

        if (p.Contains("/borrow/edit"))
            return ("Cập nhật mượn", "Cập nhật phiếu mượn");

        if (p.Contains("/borrow/return"))
            return ("Trả sách", "Xác nhận trả sách");

        // DocGia
        if (p.Contains("/docgia/create"))
            return ("Thêm độc giả", $"Thêm độc giả: {form?["HoTen"].ToString()}");

        if (p.Contains("/docgia/edit"))
            return ("Sửa độc giả", $"Cập nhật độc giả: {form?["HoTen"].ToString()}");

        if (p.Contains("/docgia/delete"))
            return ("Xóa độc giả", "Xóa độc giả");

        if (p.Contains("/docgia/import"))
            return ("Import độc giả", "Import độc giả từ Excel");

        // TheThuVien
        if (p.Contains("/thethuvien/create"))
            return ("Tạo thẻ TV", "Tạo thẻ thư viện mới");

        if (p.Contains("/thethuvien/edit"))
            return ("Sửa thẻ TV", "Cập nhật thẻ thư viện");

        if (p.Contains("/thethuvien/delete"))
            return ("Xóa thẻ TV", "Xóa thẻ thư viện");

        // NhanVien
        if (p.Contains("/nhanvien/create"))
            return ("Thêm nhân viên", $"Thêm nhân viên: {form?["HoTen"].ToString()}");

        if (p.Contains("/nhanvien/edit"))
            return ("Sửa nhân viên", $"Cập nhật nhân viên: {form?["HoTen"].ToString()}");

        if (p.Contains("/nhanvien/delete"))
            return ("Xóa nhân viên", "Xóa nhân viên");

        // TacGia
        if (p.Contains("/tacgia/create"))
            return ("Thêm tác giả", $"Thêm tác giả: {form?["TenTacGia"].ToString()}");

        if (p.Contains("/tacgia/edit"))
            return ("Sửa tác giả", $"Cập nhật tác giả: {form?["TenTacGia"].ToString()}");

        // NXB
        if (p.Contains("/nhaxuatban/create"))
            return ("Thêm NXB", $"Thêm nhà xuất bản: {form?["TenNXB"].ToString()}");

        if (p.Contains("/nhaxuatban/edit"))
            return ("Sửa NXB", $"Cập nhật NXB: {form?["TenNXB"].ToString()}");

        // Kho
        if (p.Contains("/kho"))
            return ("Nhập kho", "Cập nhật tồn kho sách");

        return (null, "");
    }
}
