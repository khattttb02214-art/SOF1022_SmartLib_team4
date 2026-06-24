using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Serilog;
using SmartLib.Web.Data;
using SmartLib.Web.Hubs;
using SmartLib.Web.Interfaces;
using SmartLib.Web.Middleware;
using SmartLib.Web.Models;
using SmartLib.Web.Services;
using SmartLib.Web.Services.Pdf;

QuestPDF.Settings.License = LicenseType.Community;


// Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();


// MVC + SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();


// Database
builder.Services.AddDbContext<SmartLibDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});


// Authentication
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)

.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";

    options.ExpireTimeSpan =
        TimeSpan.FromHours(8);

    options.SlidingExpiration = true;
});


// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Services
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<BorrowReceiptPdfService>();

builder.Services.AddScoped<AuditService>();

builder.Services.AddScoped<EmailService>();



var app = builder.Build();



// ===============================
// AUTO MIGRATE + CREATE ADMIN
// ===============================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<SmartLibDbContext>();


    // chạy migration
    db.Database.Migrate();


    // tạo chức vụ ADMIN nếu chưa có
    if (!db.ChucVus.Any(x => x.MaChucVu == "ADMIN"))
    {
        db.ChucVus.Add(new ChucVu
        {
            MaChucVu = "ADMIN",
            TenChucVu = "Quản trị viên"
        });

        db.SaveChanges();
    }



    // tạo admin nếu chưa có
    var adminExists = db.NhanViens
        .Any(x => x.MaNV == "NV001"
               || x.Email == "admin@smartlib.com");


    if (!adminExists)
    {

        var admin = new NhanVien
        {
            MaNV = "NV001",

            HoTen = "Admin System",

            Email = "admin@smartlib.com",

            MatKhau = BCrypt.Net.BCrypt.HashPassword("123456"),

            MaChucVu = "ADMIN",

            TrangThai = true,

            EmailVerified = true,

            NgayTao = DateTime.Now,

            NgayCapNhat = DateTime.Now
        };


        db.NhanViens.Add(admin);

        db.SaveChanges();
    }
}



// ===============================
// Middleware
// ===============================

app.UseStaticFiles();


app.UseRouting();


app.UseSession();


app.UseAuthentication();


app.UseAuthorization();


app.UseMiddleware<RequestLoggingMiddleware>();



// ===============================
// Route
// ===============================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


app.MapHub<NotificationHub>(
    "/notificationHub"
);



app.Run();