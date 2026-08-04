using Microsoft.AspNetCore.Mvc;
using SmartLib.Web.Attributes;
using SmartLib.Web.Interfaces;

namespace SmartLib.Web.Controllers;

/// <summary>
/// Tìm sách bằng ngôn ngữ tự nhiên (AI Search) — công khai, không yêu cầu đăng nhập, giống hệt
/// HomeController.Search (tìm kiếm truyền thống) vẫn được giữ nguyên song song để người dùng
/// lựa chọn.
///
/// Controller CHỈ điều phối: nhận từ khóa/câu hỏi từ query string rồi gọi AISearchService — không
/// chứa bất kỳ logic phân tích câu hỏi hay truy vấn dữ liệu nào (toàn bộ nằm ở AISearchService →
/// ISachService → ISachRepository).
///
/// [BoQuaPhanQuyen]: đây là tính năng công khai dùng chung, không thuộc về 1 chức năng riêng nào
/// trong ma trận phân quyền chi tiết (PhanQuyenNhanVien).
/// </summary>
[BoQuaPhanQuyen]
public class AISearchController : Controller
{
    private readonly IAISearchService _aiSearchService;

    public AISearchController(IAISearchService aiSearchService)
    {
        _aiSearchService = aiSearchService;
    }

    // GET /AISearch?q=...
    public async Task<IActionResult> Index(string? q)
    {
        var ketQua = await _aiSearchService.TimKiemAsync(q ?? string.Empty);
        return View(ketQua);
    }
}
