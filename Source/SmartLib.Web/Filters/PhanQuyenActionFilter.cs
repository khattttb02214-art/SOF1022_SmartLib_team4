using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Attributes;
using SmartLib.Web.Data;
using SmartLib.Web.Models;

namespace SmartLib.Web.Filters;

public class PhanQuyenActionFilter : IAsyncActionFilter
{
    private readonly SmartLibDbContext _db;

    // Các Controller luôn được miễn kiểm tra, kể cả khi lỡ có ChucNang trùng tên
    // (an toàn tối thiểu — không bao giờ được khoá đăng nhập/đăng xuất).
    private static readonly HashSet<string> ControllerLuonMienTru =
        new(StringComparer.OrdinalIgnoreCase) { "Auth" };

    public PhanQuyenActionFilter(SmartLibDbContext db) => _db = db;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var controllerName = context.RouteData.Values["controller"]?.ToString();

        if (user.Identity?.IsAuthenticated != true
            || user.IsInRole("ADMIN")
            || user.IsInRole("STU")   // Sinh viên cũng là bản ghi NhanVien (nên cũng có claim MaNV) nhưng
                                      // ma trận PhanQuyenNhanVien chỉ dành cho nhân viên (ADMIN/LIB) — sinh
                                      // viên KHÔNG BAO GIỜ được đối chiếu với ma trận này.
            || string.IsNullOrEmpty(controllerName)
            || ControllerLuonMienTru.Contains(controllerName)
            || context.ActionDescriptor.EndpointMetadata.OfType<BoQuaPhanQuyenAttribute>().Any())
        {
            await next();
            return;
        }

        var maNV = user.FindFirst("MaNV")?.Value;
        if (string.IsNullOrEmpty(maNV))
        {
            // Không phải phiên nhân viên/sinh viên có MaNV hợp lệ → bỏ qua an toàn.
            await next();
            return;
        }

        var chucNang = await XacDinhChucNangAsync(context, controllerName);

        if (chucNang == null)
        {
            // Controller này chưa được đăng ký vào 1 chức năng nào trong ma trận
            // phân quyền → không giới hạn (tránh khoá nhầm các trang chưa cấu hình).
            await next();
            return;
        }

        var loaiQuyen = XacDinhLoaiQuyen(context);

        var quyen = await _db.PhanQuyenNhanViens.AsNoTracking()
            .FirstOrDefaultAsync(p => p.MaNV == maNV && p.MaChucNang == chucNang.MaChucNang);

        bool duocPhep = loaiQuyen switch
        {
            LoaiQuyen.Xem => quyen?.DuocXem ?? false,
            LoaiQuyen.Them => quyen?.DuocThem ?? false,
            LoaiQuyen.Sua => quyen?.DuocSua ?? false,
            LoaiQuyen.Xoa => quyen?.DuocXoa ?? false,
            _ => false
        };

        if (!duocPhep)
        {
            TuChoi(context, chucNang.TenChucNang, loaiQuyen);
            return; // KHÔNG gọi next() → action không được thực thi.
        }

        await next();
    }

    /// <summary>
    /// Xác định dòng ChucNang tương ứng với action hiện tại:
    ///   1) Có [ThuocChucNang(id)] gắn tường minh → lấy đúng theo MaChucNang đó.
    ///   2) Không có → khớp theo Controller (mặc định). CHỈ đáng tin khi Controller
    ///      này chỉ có DUY NHẤT 1 dòng ChucNang trỏ vào — nếu 1 Controller có nhiều
    ///      dòng ChucNang (VD: BooksController vừa "Quản lý sách" vừa "Thể loại sách"),
    ///      các action thuộc dòng KHÔNG PHẢI mặc định bắt buộc phải gắn [ThuocChucNang].
    /// </summary>
    private async Task<ChucNang?> XacDinhChucNangAsync(ActionExecutingContext context, string controllerName)
    {
        var overrideAttr = context.ActionDescriptor.EndpointMetadata.OfType<ThuocChucNangAttribute>().FirstOrDefault();
        if (overrideAttr != null)
        {
            return await _db.ChucNangs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.MaChucNang == overrideAttr.MaChucNang);
        }

        // .OrderBy(MaChucNang) để đảm bảo kết quả ỔN ĐỊNH khi 1 Controller có nhiều
        // dòng ChucNang trỏ vào (VD: NhanVien có cả dòng 11 "Quản lý nhân viên" lẫn
        // dòng 12 "Duyệt tài khoản đăng ký") — dòng có MaChucNang nhỏ nhất thắng làm
        // mặc định, còn (các) dòng còn lại BẮT BUỘC phải gắn [ThuocChucNang] mới tới lượt.
        return await _db.ChucNangs.AsNoTracking()
            .Where(c => c.Controller == controllerName)
            .OrderBy(c => c.MaChucNang)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Xác định action hiện tại cần loại quyền nào, theo thứ tự ưu tiên:
    ///   1) Attribute [ChucNangQuyen(...)] gắn tường minh trên action/controller.
    ///   2) Từ khoá trong tên action (khớp quy ước đặt tên Create/Edit/Delete/Xoa/Them/Sua...).
    ///   3) Không khớp từ khoá nào → suy theo HTTP verb: GET = Xem, còn lại = Sửa
    ///      (mặc định an toàn: 1 action lạ chưa đặt tên rõ ràng thì coi như có thay
    ///      đổi dữ liệu, cần quyền Sửa trở lên, thay vì mặc định cho Xem).
    /// </summary>
    private static LoaiQuyen XacDinhLoaiQuyen(ActionExecutingContext context)
    {
        var attr = context.ActionDescriptor.EndpointMetadata.OfType<ChucNangQuyenAttribute>().FirstOrDefault();
        if (attr != null) return attr.Loai;

        var action = (context.RouteData.Values["action"]?.ToString() ?? "").ToLowerInvariant();

        string[] tuKhoaXoa = { "delete", "xoa", "remove", "reject", "tuchoi" };
        string[] tuKhoaThem = { "create", "them", "tao", "add", "import", "nhap" };
        string[] tuKhoaSua = { "edit", "sua", "update", "toggle", "doi", "approve",
                                "duyet", "sync", "return", "renew", "giahan", "save", "cancel", "huy" };

        if (tuKhoaXoa.Any(action.Contains)) return LoaiQuyen.Xoa;
        if (tuKhoaThem.Any(action.Contains)) return LoaiQuyen.Them;
        if (tuKhoaSua.Any(action.Contains)) return LoaiQuyen.Sua;

        var method = context.HttpContext.Request.Method;
        return HttpMethods.IsGet(method) ? LoaiQuyen.Xem : LoaiQuyen.Sua;
    }

    private static void TuChoi(ActionExecutingContext context, string tenChucNang, LoaiQuyen loai)
    {
        var tenQuyen = loai switch
        {
            LoaiQuyen.Xem => "Xem",
            LoaiQuyen.Them => "Thêm",
            LoaiQuyen.Sua => "Sửa",
            LoaiQuyen.Xoa => "Xóa",
            _ => ""
        };
        var message = $"Bạn không có quyền {tenQuyen} ở chức năng \"{tenChucNang}\". Vui lòng liên hệ quản trị viên nếu cần cấp thêm quyền.";
        var httpContext = context.HttpContext;

        if (LaAjaxRequest(httpContext.Request))
        {
            // Trả JSON cùng dạng {success:false, message:...} mà các endpoint AJAX
            // khác trong hệ thống (VD: PhanQuyenController) đang dùng, để code JS
            // hiện có (.done()/.fail() đọc field success/message) xử lý bình thường.
            context.Result = new JsonResult(new { success = false, message });
            return;
        }

        var tempDataFactory = httpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
        var tempData = tempDataFactory.GetTempData(httpContext);
        tempData["error"] = message;
        tempData.Save();

        var referer = httpContext.Request.Headers["Referer"].ToString();
        var target = "/";
        if (!string.IsNullOrEmpty(referer) &&
            Uri.TryCreate(referer, UriKind.Absolute, out var refererUri) &&
            refererUri.Host == httpContext.Request.Host.Host)
        {
            target = referer;
        }

        context.Result = new RedirectResult(target);
    }

    private static bool LaAjaxRequest(HttpRequest request) =>
        request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
        request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
        request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
}
