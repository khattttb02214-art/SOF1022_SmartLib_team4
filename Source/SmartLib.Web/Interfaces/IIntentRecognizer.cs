namespace SmartLib.Web.Interfaces;

/// <summary>Kết quả nhận diện ý định: loại ý định + tham số trích xuất được (VD: tên sách trong dấu ngoặc kép).</summary>
public record KetQuaNhanDien(ChatIntent YDinh, string? ThamSo);

/// <summary>
/// Nhận diện ý định (intent) của người dùng từ 1 câu hỏi tiếng Việt tự nhiên.
///
/// Cài đặt mặc định (<see cref="SmartLib.Web.Services.AI.IntentRecognizer"/>) dùng kỹ thuật
/// so khớp từ khóa/regex đã chuẩn hóa (bỏ dấu, viết thường) — KHÔNG phụ thuộc dịch vụ AI trả phí
/// bên ngoài, để đảm bảo tính năng luôn hoạt động ổn định (không cần API key, không cần Internet,
/// không phát sinh chi phí) và cho kết quả xác định (deterministic), dễ kiểm thử.
///
/// Nếu sau này muốn nâng cấp lên mô hình AI/NLP thông minh hơn, chỉ cần viết thêm 1 class khác
/// cài đặt interface này rồi đăng ký lại trong Program.cs — phần còn lại của hệ thống không cần
/// thay đổi gì (nhờ lập trình theo interface).
/// </summary>
public interface IIntentRecognizer
{
    KetQuaNhanDien NhanDien(string cauHoi);
}
