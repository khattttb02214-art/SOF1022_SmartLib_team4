using SmartLib.Web.Interfaces;
using SmartLib.Web.Models;

namespace SmartLib.Web.Services.AI;

/// <summary>
/// Cài đặt IAISearchService: nhận câu hỏi tìm sách tự nhiên, nhờ IKeywordExpander sinh từ khóa,
/// rồi gọi ISachService (BookService) để tìm dữ liệu thật. Nếu không có kết quả khớp trực tiếp,
/// tự động chuyển sang đề xuất sách có tên GẦN GIỐNG nhất (xử lý cả trường hợp gõ sai chính tả).
/// </summary>
public class AISearchService : IAISearchService
{
    private readonly IKeywordExpander _tuKhoaExpander;
    private readonly ISachService _sachService;

    private const int SoLuongKetQuaToiDa = 20;
    private const int SoLuongGoiYGanGiong = 5;

    public AISearchService(IKeywordExpander tuKhoaExpander, ISachService sachService)
    {
        _tuKhoaExpander = tuKhoaExpander;
        _sachService = sachService;
    }

    public async Task<KetQuaTimKiemAI> TimKiemAsync(string cauHoi)
    {
        cauHoi = (cauHoi ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(cauHoi))
            return new KetQuaTimKiemAI(cauHoi, new List<string>(), new List<Sach>(), false);

        // Bước 1: AI phân tích câu hỏi → sinh danh sách từ khóa tìm kiếm mở rộng.
        var tuKhoas = _tuKhoaExpander.MoRongTuKhoa(cauHoi);

        // Bước 2: gọi BookService để tìm dữ liệu thật theo các từ khóa đó.
        var ketQuaTrucTiep = await _sachService.TimTheoNhieuTuKhoaAsync(tuKhoas, SoLuongKetQuaToiDa);

        if (ketQuaTrucTiep.Count > 0)
            return new KetQuaTimKiemAI(cauHoi, tuKhoas, ketQuaTrucTiep, LaGoiYGanGiong: false);

        // Bước 3: không có kết quả khớp trực tiếp → đề xuất sách có TÊN gần giống nhất, xử lý
        // được cả trường hợp gõ sai chính tả (VD: "Clen Code" → gợi ý "Clean Code").
        var tuKhoaGanGiong = tuKhoas.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? cauHoi;
        var goiY = await _sachService.TimSachGanGiongAsync(tuKhoaGanGiong, SoLuongGoiYGanGiong);

        return new KetQuaTimKiemAI(cauHoi, tuKhoas, goiY, LaGoiYGanGiong: true);
    }
}
