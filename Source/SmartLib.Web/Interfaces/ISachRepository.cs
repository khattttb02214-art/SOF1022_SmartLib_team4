using SmartLib.Web.Models;

namespace SmartLib.Web.Interfaces;

/// <summary>
/// Tầng Repository cho dữ liệu Sách — lớp DUY NHẤT được phép thao tác trực tiếp với
/// SmartLibDbContext cho các truy vấn Sách phục vụ SachService (BookService). SachService
/// KHÔNG được tự ý truy vấn DbContext, mọi thứ phải đi qua đây, đúng mô hình:
///
///     Controller → Service → Repository → Database
/// </summary>
public interface ISachRepository
{
    /// <summary>Lấy 1 sách theo đúng mã, kèm Thể loại/NXB/Tác giả.</summary>
    Task<Sach?> LayTheoMaAsync(string maSach);

    /// <summary>
    /// Tìm sách còn hoạt động có TÊN, THỂ LOẠI hoặc TÊN TÁC GIẢ khớp (gần đúng) với 1 từ khóa,
    /// kèm đầy đủ dữ liệu hiển thị (Thể loại, NXB, Tác giả).
    /// </summary>
    Task<List<Sach>> TimTheoTuKhoaAsync(string tuKhoa, int soLuongToiDa);

    /// <summary>
    /// Lấy nhẹ (chỉ Mã + Tên) của TOÀN BỘ sách còn hoạt động — dùng cho bước so khớp gần giống
    /// (Levenshtein) ở tầng Service, KHÔNG kèm các cột nặng (ảnh bìa, mô tả...) để tiết kiệm bộ nhớ.
    /// </summary>
    Task<List<(string MaSach, string TenSach)>> LayDanhSachTenSachAsync();

    /// <summary>Lấy đầy đủ dữ liệu hiển thị của các sách theo danh sách mã cụ thể (không đảm bảo thứ tự trả về).</summary>
    Task<List<Sach>> LayTheoDanhSachMaAsync(List<string> maSachs);
}
