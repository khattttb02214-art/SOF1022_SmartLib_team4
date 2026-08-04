using SmartLib.Web.Models;

namespace SmartLib.Web.Interfaces;

/// <summary>Kết quả trả về của 1 lượt tìm kiếm bằng AI.</summary>
/// <param name="CauHoiGoc">Câu hỏi/từ khóa gốc người dùng đã nhập.</param>
/// <param name="TuKhoaDaTim">Danh sách từ khóa AI đã dùng để tìm (hiển thị minh bạch cho người dùng biết AI hiểu câu hỏi thế nào).</param>
/// <param name="DanhSachSach">Danh sách sách kết quả (đã kèm Thể loại/NXB/Tác giả để hiển thị).</param>
/// <param name="LaGoiYGanGiong">true nếu đây là các sách GỢI Ý GẦN GIỐNG (do không tìm thấy kết quả khớp trực tiếp), false nếu là kết quả khớp trực tiếp.</param>
public record KetQuaTimKiemAI(
    string CauHoiGoc,
    List<string> TuKhoaDaTim,
    List<Sach> DanhSachSach,
    bool LaGoiYGanGiong);

/// <summary>
/// "Bộ não" điều phối của chức năng AI Search — nhận câu hỏi tìm sách bằng ngôn ngữ tự nhiên,
/// nhờ IKeywordExpander sinh từ khóa tìm kiếm, rồi LẤY DỮ LIỆU THẬT thông qua ISachService
/// (BookService) để tìm sách — không bao giờ tự truy vấn DbContext hay Repository trực tiếp.
///
/// Luồng xử lý đúng theo kiến trúc yêu cầu:
///     Controller → <b>AISearchService</b> → ISachService (BookService) → ISachRepository → Database.
/// </summary>
public interface IAISearchService
{
    Task<KetQuaTimKiemAI> TimKiemAsync(string cauHoi);
}
