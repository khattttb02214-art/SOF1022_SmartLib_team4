using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Data;
using SmartLib.Web.Models;

public static class DbSeeder
{
    public static void Seed(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartLibDbContext>();

        db.Database.Migrate(); // 🔥 tự update DB

        if (!db.NhanViens.Any(x => x.Email == "admin@smartlib.com"))
        {
            db.NhanViens.Add(new NhanVien
            {
                MaNV = "NV001",
                HoTen = "Admin System",
                Email = "admin@smartlib.com",
                MatKhau = "123456",
                MaChucVu = "ADMIN",
                TrangThai = true,
                EmailVerified = true
                // Không cần NgayTao vì DB default lo
            });

            db.SaveChanges();
        }
    }
}