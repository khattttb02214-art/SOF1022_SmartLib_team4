using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SmartLib.Web.Data;
using SmartLib.Web.Hubs;
using SmartLib.Web.Interfaces;
using SmartLib.Web.Filters;
using SmartLib.Web.Middleware;
using SmartLib.Web.Repositories;
using SmartLib.Web.Services;
using SmartLib.Web.Services.Pdf;
using SmartLib.Web.Services.AI;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllersWithViews(options =>
{
    // Bắt buộc mọi action đi qua kiểm tra ma trận phân quyền chi tiết (PhanQuyenNhanVien).
    options.Filters.Add<PhanQuyenActionFilter>();
});
builder.Services.AddSignalR();

builder.Services.AddDbContext<SmartLibDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt => {
        opt.LoginPath = "/Auth/Login";
        opt.LogoutPath = "/Auth/Logout";
        opt.AccessDeniedPath = "/Auth/AccessDenied";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, opt => {
        opt.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        opt.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        opt.CallbackPath = "/signin-google";
        opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opt.SaveTokens = false;
    });

// Cho phép xác thực CSRF token qua header (dùng cho các lời gọi AJAX/fetch JSON,
// ví dụ màn hình Phân quyền) — không ảnh hưởng đến các <form> hiện có vẫn dùng field ẩn.
builder.Services.AddAntiforgery(opt => opt.HeaderName = "RequestVerificationToken");

builder.Services.AddSession(opt => {
    opt.IdleTimeout = TimeSpan.FromMinutes(30);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<BorrowReceiptPdfService>();
builder.Services.AddScoped<SmartLib.Web.Services.AuditService>();
builder.Services.AddScoped<SmartLib.Web.Services.EmailService>();

// ── AI Assistant (Chatbot thư viện) ─────────────────────────────────────────
// AIService KHÔNG được phép tự truy vấn DbContext — mọi dữ liệu thật (sách, mượn/trả, đặt
// trước) đều phải đi qua ISachService/IMuonTraService/IReservationService bên dưới.
// Xem thêm: Controllers/AIController.cs, Services/AI/AIService.cs, Services/AI/IntentRecognizer.cs
builder.Services.AddScoped<ISachRepository, SachRepository>();
builder.Services.AddScoped<ISachService, SachService>();
builder.Services.AddScoped<IMuonTraService, MuonTraService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IIntentRecognizer, IntentRecognizer>();
builder.Services.AddScoped<IAIService, AIService>();

// ── AI Search (tìm sách bằng ngôn ngữ tự nhiên) ─────────────────────────────
// Controller → AISearchService → ISachService (BookService) → ISachRepository → Database.
// Xem thêm: Controllers/AISearchController.cs, Services/AI/AISearchService.cs, Services/AI/KeywordExpander.cs
builder.Services.AddScoped<IKeywordExpander, KeywordExpander>();
builder.Services.AddScoped<IAISearchService, AISearchService>();

var app = builder.Build();

// Đặt ĐẦU TIÊN trong pipeline để bọc toàn bộ middleware/controller phía sau:
// bất kỳ exception chưa xử lý nào (kể cả lỗi xóa dữ liệu vướng khóa ngoại,
// lỗi trong chức năng phân quyền, v.v...) sẽ được chuyển thành thông báo
// (toastr) thân thiện thay vì làm đứng web.
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
