using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SmartLib.Web.Middleware;

/// <summary>
/// Middleware bắt TOÀN BỘ exception chưa được xử lý ở bất kỳ đâu trong pipeline
/// (bao gồm cả các lỗi phát sinh từ chức năng phân quyền, xóa dữ liệu vướng
/// khóa ngoại, v.v...). Mục tiêu: web KHÔNG BAO GIỜ bị đứng/crash vì 1 lỗi
/// chưa xử lý — thay vào đó luôn trả về một thông báo (toastr) thân thiện,
/// còn lỗi thật vẫn được ghi đầy đủ vào log (Serilog) để debug sau.
///
/// Đặt middleware này ĐẦU TIÊN trong pipeline (Program.cs) để nó bọc được
/// toàn bộ các middleware/controller phía sau.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi chưa xử lý tại {Method} {Path}", context.Request.Method, context.Request.Path);

            // Nếu response đã bắt đầu ghi dữ liệu ra (đã stream) thì không thể
            // redirect/ghi JSON nữa — đành ném lại để middleware mặc định xử lý.
            if (context.Response.HasStarted)
                throw;

            var message = BuildFriendlyMessage(ex);

            if (IsAjaxRequest(context.Request))
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { success = false, message });
                return;
            }

            // Request thường (click link / submit form) → lưu thông báo lỗi vào
            // TempData rồi quay lại trang trước đó. _Layout.cshtml sẽ tự hiển thị
            // TempData["error"] bằng toastr.error(...) như các chỗ khác trong app.
            // LƯU Ý: Clear() phải gọi TRƯỚC khi lưu TempData/set Location, vì Clear()
            // sẽ xóa sạch toàn bộ header (kể cả cookie TempData vừa ghi nếu gọi sau).
            context.Response.Clear();

            var tempDataFactory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(context);
            tempData["error"] = message;
            tempData.Save();

            context.Response.Redirect(GetSafeRedirectTarget(context));
        }
    }

    /// <summary>Dựng thông báo dễ hiểu cho người dùng thay vì để lộ chi tiết kỹ thuật.</summary>
    private static string BuildFriendlyMessage(Exception ex)
    {
        if (ex is DbUpdateException)
        {
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException;

            // Lỗi 547 = vi phạm ràng buộc khóa ngoại (FOREIGN KEY / REFERENCE) hoặc CHECK constraint
            if (inner is SqlException { Number: 547 })
                return "Không thể thực hiện: dữ liệu này đang được sử dụng ở nơi khác trong hệ thống nên không thể xóa/cập nhật.";

            return "Không thể lưu thay đổi vào cơ sở dữ liệu. Vui lòng thử lại.";
        }

        return "Đã xảy ra lỗi ngoài ý muốn. Vui lòng thử lại, nếu vẫn còn lỗi hãy liên hệ quản trị viên.";
    }

    private static bool IsAjaxRequest(HttpRequest request) =>
        request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
        request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
        request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Ưu tiên quay lại trang Referer (cùng domain); nếu không có/không an toàn thì về trang chủ.</summary>
    private static string GetSafeRedirectTarget(HttpContext context)
    {
        var referer = context.Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) &&
            Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) &&
            refererUri.Host == context.Request.Host.Host)
        {
            return referer;
        }
        return "/";
    }
}
