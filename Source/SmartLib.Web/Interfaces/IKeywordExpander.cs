namespace SmartLib.Web.Interfaces;

/// <summary>
/// Phân tích 1 câu hỏi tìm sách bằng ngôn ngữ tự nhiên và sinh ra danh sách từ khóa tìm kiếm mở
/// rộng — đây là phần "trí tuệ" của AI Search.
///
/// VD: "Tôi muốn học C#" → ["C#", "ASP.NET", ".NET", "Entity Framework"]
///     "Có sách về trí tuệ nhân tạo không?" → ["Artificial Intelligence", "Machine Learning", "Deep Learning"]
///
/// Cài đặt mặc định (<see cref="SmartLib.Web.Services.AI.KeywordExpander"/>) dùng từ điển
/// chủ đề → từ khóa (rule-based), không gọi dịch vụ AI/LLM trả phí bên ngoài, cùng triết lý với
/// IIntentRecognizer của AI Assistant: luôn chạy được, không cần API key, kết quả xác định.
/// </summary>
public interface IKeywordExpander
{
    /// <summary>Sinh danh sách từ khóa tìm kiếm từ 1 câu hỏi tự nhiên. Luôn trả về ít nhất 1 từ khóa.</summary>
    List<string> MoRongTuKhoa(string cauHoi);
}
