using SmartLib.Web.Interfaces;
using SmartLib.Web.Models;
using SmartLib.Web.Services.AI;

namespace SmartLib.Web.Services;

/// <summary>
/// Cài đặt ISachService (BookService) — tầng nghiệp vụ cho dữ liệu Sách, dùng chung cho AI
/// Assistant (Chatbot) và AI Search. Theo đúng mô hình Controller → Service → Repository →
/// Database: SachService KHÔNG tự truy vấn DbContext, mọi thao tác dữ liệu đều đi qua
/// ISachRepository — SachService chỉ chứa LOGIC NGHIỆP VỤ (ưu tiên khớp chính xác, gộp/loại
/// trùng nhiều từ khóa, thuật toán tìm gần giống khi sai chính tả...).
/// </summary>
public class SachService : ISachService
{
    private readonly ISachRepository _repo;

    // Lấy dư số lượng ứng viên từ Repository (nhiều hơn số lượng cuối cùng cần trả về) để khi
    // sắp xếp ưu tiên "khớp chính xác tên" ở bước sau không bị bỏ sót sách khớp đúng nhất, phòng
    // trường hợp nó không nằm trong nhóm mới tạo gần đây nhất (Repository sắp theo NgayTao).
    private const int SoLuongUngVienToiDa = 30;

    public SachService(ISachRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<Sach>> TimKiemTheoTenAsync(string tuKhoa, int soLuongToiDa = 5)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa)) return new List<Sach>();

        var ungVien = await _repo.TimTheoTuKhoaAsync(tuKhoa, SoLuongUngVienToiDa);

        // Sách khớp CHÍNH XÁC tên được xếp lên đầu, vì 1 từ khóa có thể khớp nhiều sách
        // (VD: các tập trong 1 bộ truyện), và người hỏi thường muốn biết đúng cuốn khớp nhất trước tiên.
        return ungVien
            .OrderByDescending(s => string.Equals(s.TenSach, tuKhoa, StringComparison.OrdinalIgnoreCase))
            .ThenBy(s => s.TenSach)
            .Take(soLuongToiDa)
            .ToList();
    }

    public async Task<Sach?> LayTheoMaAsync(string maSach)
    {
        return await _repo.LayTheoMaAsync(maSach);
    }

    public async Task<List<Sach>> TimTheoNhieuTuKhoaAsync(List<string> tuKhoas, int soLuongToiDa = 20)
    {
        var ketQua = new List<Sach>();
        var daThem = new HashSet<string>();

        foreach (var tuKhoa in (tuKhoas ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            if (ketQua.Count >= soLuongToiDa) break;

            var mot = await _repo.TimTheoTuKhoaAsync(tuKhoa, soLuongToiDa);
            foreach (var s in mot)
            {
                // HashSet.Add trả về false nếu mã đã tồn tại → nhờ đó tự động loại trùng khi
                // 1 sách khớp với nhiều hơn 1 từ khóa mở rộng (VD: vừa khớp "C#" vừa khớp ".NET").
                if (daThem.Add(s.MaSach))
                    ketQua.Add(s);
            }
        }

        return ketQua.Take(soLuongToiDa).ToList();
    }

    public async Task<List<Sach>> TimSachGanGiongAsync(string tuKhoa, int soLuongToiDa = 5)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa)) return new List<Sach>();

        var tatCaTen = await _repo.LayDanhSachTenSachAsync();
        if (tatCaTen.Count == 0) return new List<Sach>();

        var tuKhoaChuan = VietnameseTextHelper.BoDauVaThuongHoa(tuKhoa);

        // Ngưỡng khoảng cách tối đa được coi là "gần giống" — tỉ lệ theo độ dài từ khóa, tối
        // thiểu 3. Tránh trường hợp từ khóa quá khác biệt với MỌI tên sách trong hệ thống (VD:
        // 1 chủ đề hoàn toàn không có trong thư viện) mà vẫn bị ép đề xuất ra sách không liên
        // quan gì — chỉ nên gợi ý khi thực sự giống (VD: gõ sai vài ký tự như "Clen"→"Clean").
        int nguongToiDa = Math.Max(3, tuKhoaChuan.Length / 2);

        // Tính khoảng cách Levenshtein giữa từ khóa và tên từng sách (đã chuẩn hóa bỏ dấu để so
        // sánh công bằng), lấy ra những sách có khoảng cách NHỎ NHẤT (giống nhất) và trong ngưỡng.
        var maGanNhatTheoThuTu = tatCaTen
            .Select(s => new
            {
                s.MaSach,
                KhoangCach = LevenshteinHelper.TinhKhoangCach(tuKhoaChuan, VietnameseTextHelper.BoDauVaThuongHoa(s.TenSach))
            })
            .Where(x => x.KhoangCach <= nguongToiDa)
            .OrderBy(x => x.KhoangCach)
            .Take(soLuongToiDa)
            .Select(x => x.MaSach)
            .ToList();

        var sachDayDu = await _repo.LayTheoDanhSachMaAsync(maGanNhatTheoThuTu);

        // LayTheoDanhSachMaAsync KHÔNG đảm bảo thứ tự trả về theo danh sách mã truyền vào, nên
        // phải xếp lại thủ công đúng thứ tự "gần giống nhất trước" đã tính ở trên.
        return maGanNhatTheoThuTu
            .Select(ma => sachDayDu.FirstOrDefault(s => s.MaSach == ma))
            .Where(s => s != null)
            .Select(s => s!)
            .ToList();
    }
}
