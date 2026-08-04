using SmartLib.Web.Models;

namespace SmartLib.Web.Interfaces;

/// <summary>
/// Các thao tác ĐỌC dữ liệu Sách phục vụ AI Assistant (và các thành phần khác nếu cần sau này).
/// AIService chỉ được phép lấy dữ liệu Sách thông qua interface này, tuyệt đối không tự
/// truy vấn DbContext trực tiếp.
/// </summary>
public interface ISachService
{
    /// <summary>
    /// Tìm các sách có tên khớp (gần đúng, không phân biệt hoa/thường) với từ khóa.
    /// Sách khớp CHÍNH XÁC tên sẽ được ưu tiên xếp lên đầu danh sách trả về.
    /// </summary>
    Task<List<Sach>> TimKiemTheoTenAsync(string tuKhoa, int soLuongToiDa = 5);

    /// <summary>Lấy 1 sách theo đúng mã sách.</summary>
    Task<Sach?> LayTheoMaAsync(string maSach);

    /// <summary>
    /// Tìm sách khớp với BẤT KỲ từ khóa nào trong danh sách (dùng cho AI Search: 1 câu hỏi có
    /// thể được AI mở rộng thành nhiều từ khóa liên quan, VD "C#" → "C#, ASP.NET, .NET, Entity
    /// Framework"). Kết quả đã được gộp và loại trùng theo MaSach.
    /// </summary>
    Task<List<Sach>> TimTheoNhieuTuKhoaAsync(List<string> tuKhoas, int soLuongToiDa = 20);

    /// <summary>
    /// Tìm các sách có TÊN gần giống nhất với 1 từ khóa (dùng thuật toán Levenshtein) — dùng khi
    /// tìm kiếm trực tiếp không ra kết quả, kể cả trường hợp người dùng gõ sai chính tả
    /// (VD: "Clen Code" vẫn gợi ý ra "Clean Code").
    /// </summary>
    Task<List<Sach>> TimSachGanGiongAsync(string tuKhoa, int soLuongToiDa = 5);
}
