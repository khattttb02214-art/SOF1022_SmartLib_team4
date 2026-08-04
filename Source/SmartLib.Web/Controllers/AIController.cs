using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartLib.Web.Attributes;
using SmartLib.Web.Interfaces;
using SmartLib.Web.ViewModels;

namespace SmartLib.Web.Controllers;

/// <summary>
/// API cho widget "AI Assistant" (Chatbot thư viện) hiển thị ở góc phải màn hình.
///
/// Controller này CHỈ điều phối: đọc thông tin người dùng từ Claims đăng nhập, gọi AIService để
/// lấy câu trả lời, rồi lưu/đọc lịch sử chat trong Session. Toàn bộ logic nhận diện ý định và
/// truy vấn dữ liệu thư viện nằm trong AIService + các Service khác — Controller không đụng gì
/// đến DbContext.
///
/// [Authorize] : bắt buộc đăng nhập vì AI cần biết đang trả lời cho ai (MaDocGia/vai trò).
/// [BoQuaPhanQuyen] : đây là tính năng phụ trợ dùng chung cho MỌI vai trò (Sinh viên/Thủ thư/
/// Admin), không thuộc về 1 chức năng riêng nào trong ma trận phân quyền chi tiết
/// (PhanQuyenNhanVien) nên được loại trừ khỏi PhanQuyenActionFilter.
/// </summary>
[Authorize]
[BoQuaPhanQuyen]
public class AIController : Controller
{
    private const string SessionKey = "AIChat_LichSu";
    private const int SoTinNhanToiDaLuu = 100; // tránh Session phình to nếu chat rất dài trong 1 phiên

    private readonly IAIService _aiService;

    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>Gửi 1 câu hỏi cho AI Assistant; trả lời JSON và lưu cả 2 chiều hội thoại vào Session.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] ChatRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return Json(new { success = false, message = "Vui lòng nhập câu hỏi." });

        var cauHoi = request.Message.Trim();
        var nguoiDung = LayThongTinNguoiDungHienTai();
        var traLoi = await _aiService.TraLoiAsync(cauHoi, nguoiDung);

        var lichSu = DocLichSuTuSession();
        var bayGio = DateTime.Now;
        lichSu.Add(new ChatMessageVM { Role = "user", NoiDung = cauHoi, ThoiGian = bayGio });
        lichSu.Add(new ChatMessageVM { Role = "ai", NoiDung = traLoi, ThoiGian = bayGio });

        if (lichSu.Count > SoTinNhanToiDaLuu)
            lichSu = lichSu.Skip(lichSu.Count - SoTinNhanToiDaLuu).ToList();

        GhiLichSuVaoSession(lichSu);

        return Json(new { success = true, reply = traLoi, thoiGian = bayGio });
    }

    /// <summary>Lấy lại lịch sử chat đã lưu trong Session (VD: khi người dùng chuyển trang rồi mở lại khung chat).</summary>
    [HttpGet]
    public IActionResult LichSu()
    {
        return Json(new { success = true, tinNhan = DocLichSuTuSession() });
    }

    /// <summary>Xóa toàn bộ lịch sử chat trong Session (nút "Xóa hội thoại" trên giao diện).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult XoaLichSu()
    {
        HttpContext.Session.Remove(SessionKey);
        return Json(new { success = true });
    }

    /// <summary>Trích thông tin người dùng hiện tại từ Claims đăng nhập (xem AuthController để đối chiếu tên Claim).</summary>
    private NguoiDungHienTai LayThongTinNguoiDungHienTai()
    {
        var maNV = User.FindFirst("MaNV")?.Value ?? "";
        var maDocGia = User.FindFirst("MaDocGia")?.Value;
        var hoTen = User.Identity?.Name ?? "bạn";

        return new NguoiDungHienTai(
            MaNV: maNV,
            MaDocGia: string.IsNullOrEmpty(maDocGia) ? null : maDocGia,
            HoTen: hoTen,
            LaSinhVien: User.IsInRole("STU"),
            LaThuThu: User.IsInRole("LIB"),
            LaAdmin: User.IsInRole("ADMIN"));
    }

    private List<ChatMessageVM> DocLichSuTuSession()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new List<ChatMessageVM>();

        try
        {
            return JsonSerializer.Deserialize<List<ChatMessageVM>>(json) ?? new List<ChatMessageVM>();
        }
        catch
        {
            // Dữ liệu Session bị hỏng/sai định dạng (rất hiếm) — bỏ qua, coi như chưa có lịch sử.
            return new List<ChatMessageVM>();
        }
    }

    private void GhiLichSuVaoSession(List<ChatMessageVM> lichSu)
    {
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(lichSu));
    }
}
