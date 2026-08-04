namespace SmartLib.Web.Interfaces;

/// <summary>
/// Thông tin tối thiểu về người đang chat, do Controller trích xuất từ Claims đăng nhập rồi
/// truyền vào AIService. AIService KHÔNG phụ thuộc trực tiếp vào HttpContext/ClaimsPrincipal
/// để dễ kiểm thử (unit test) và tách biệt khỏi hạ tầng web (ASP.NET Core).
/// </summary>
/// <param name="MaNV">Mã tài khoản đăng nhập (luôn có).</param>
/// <param name="MaDocGia">Mã độc giả liên kết (chỉ có với tài khoản Sinh viên); null nếu là tài khoản nhân viên thuần túy.</param>
/// <param name="HoTen">Họ tên hiển thị, dùng để xưng hô thân thiện.</param>
/// <param name="LaSinhVien">true nếu role hiện tại là STU.</param>
/// <param name="LaThuThu">true nếu role hiện tại là LIB.</param>
/// <param name="LaAdmin">true nếu role hiện tại là ADMIN.</param>
public record NguoiDungHienTai(
    string MaNV,
    string? MaDocGia,
    string HoTen,
    bool LaSinhVien,
    bool LaThuThu,
    bool LaAdmin);

/// <summary>
/// "Bộ não" điều phối của chức năng AI Assistant (Chatbot thư viện SmartLib).
///
/// Luồng xử lý: Controller → <b>AIService</b> → ISachService/IMuonTraService/IReservationService → Database.
/// AIService KHÔNG BAO GIỜ tự truy vấn DbContext — mọi dữ liệu thật của hệ thống đều phải lấy
/// thông qua 3 Service nói trên, theo đúng yêu cầu kiến trúc.
/// </summary>
public interface IAIService
{
    /// <summary>Trả lời 1 câu hỏi tự nhiên của người dùng dựa trên dữ liệu thật + quy định thư viện.</summary>
    Task<string> TraLoiAsync(string cauHoi, NguoiDungHienTai nguoiDung);
}
