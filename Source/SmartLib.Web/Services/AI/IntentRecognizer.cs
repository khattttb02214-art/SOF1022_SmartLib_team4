using System.Text.RegularExpressions;
using SmartLib.Web.Interfaces;

namespace SmartLib.Web.Services.AI;

/// <summary>
/// Cài đặt IIntentRecognizer bằng kỹ thuật so khớp từ khóa/regex trên văn bản đã chuẩn hóa
/// (bỏ dấu tiếng Việt + viết thường). Lý do chọn cách tiếp cận này thay vì gọi 1 dịch vụ AI/LLM
/// bên ngoài:
///   1. Không cần API key, không phụ thuộc Internet, không phát sinh chi phí — luôn chạy được.
///   2. Kết quả xác định (deterministic), dễ kiểm thử, dễ giải thích khi bảo vệ đồ án.
///   3. Người dùng Việt Nam khi chat thường gõ KHÔNG dấu ("sach nay con khong") — việc chuẩn hóa
///      bỏ dấu giúp nhận diện đúng ý định trong cả 2 trường hợp có dấu và không dấu.
///
/// Nếu muốn nâng cấp lên mô hình AI/NLP thông minh hơn sau này, chỉ cần viết 1 class khác cài
/// đặt IIntentRecognizer rồi đổi đăng ký DI trong Program.cs — AIService và các Controller
/// không cần sửa gì cả.
/// </summary>
public class IntentRecognizer : IIntentRecognizer
{
    public KetQuaNhanDien NhanDien(string cauHoi)
    {
        if (string.IsNullOrWhiteSpace(cauHoi))
            return new KetQuaNhanDien(ChatIntent.NgoaiPhamVi, null);

        var goc = cauHoi.Trim();
        var chuan = VietnameseTextHelper.BoDauVaThuongHoa(goc);

        // 1) Quy định mượn trả — kiểm tra sớm vì khá đặc trưng, tránh bị các nhánh khác "cướp" trước.
        if (Chua(chuan, "quy dinh") || Chua(chuan, "noi quy") || Chua(chuan, "quy che muon"))
            return new KetQuaNhanDien(ChatIntent.QuyDinhMuonTra, null);

        // 2) Gia hạn sách.
        if (Chua(chuan, "gia han") || Chua(chuan, "renew"))
            return new KetQuaNhanDien(ChatIntent.CoTheGiaHanKhong, TrichTenSach(goc));

        // 3) Thống kê (Thủ thư): tổng số phiếu đang quá hạn — phải kiểm tra TRƯỚC ý định
        //    "tôi quá hạn chưa" vì cả 2 đều chứa từ khóa "qua han".
        if (Chua(chuan, "qua han") && ChuaBatKy(chuan, "bao nhieu", "so luong", "tong so", "co may"))
            return new KetQuaNhanDien(ChatIntent.SoSachDangQuaHan, null);

        // 4) Quá hạn cá nhân ("trễ" là từ đồng nghĩa thông dụng của "quá hạn").
        if (Chua(chuan, "qua han") || Chua(chuan, "tre han") || (Chua(chuan, "tre") && Chua(chuan, "sach")))
            return new KetQuaNhanDien(ChatIntent.DaQuaHanChua, null);

        // 5) Thống kê (Thủ thư): số lượt mượn hôm nay.
        if ((Chua(chuan, "luot muon") || (Chua(chuan, "muon") && Chua(chuan, "hom nay")))
            && ChuaBatKy(chuan, "bao nhieu", "so luong", "co may"))
            return new KetQuaNhanDien(ChatIntent.SoLuotMuonHomNay, null);

        // 6) Thống kê (Thủ thư): sách được mượn nhiều nhất.
        if (Chua(chuan, "nhieu nhat") && (Chua(chuan, "muon") || Chua(chuan, "hot")))
            return new KetQuaNhanDien(ChatIntent.SachDuocMuonNhieuNhat, null);

        // 7) Thống kê (Thủ thư): tổng số đặt trước — phải kiểm tra TRƯỚC ý định "tôi đặt trước sách gì".
        if (Chua(chuan, "dat truoc") && ChuaBatKy(chuan, "bao nhieu", "so luong", "co may"))
            return new KetQuaNhanDien(ChatIntent.SoSachDangDatTruoc, null);

        // 8) Đặt trước cá nhân.
        if (Chua(chuan, "dat truoc"))
            return new KetQuaNhanDien(ChatIntent.DaDatTruocSachGi, null);

        // 9) Sách còn hay hết (theo tên cụ thể, thường được nhắc trong dấu ngoặc kép).
        var tenTrongNgoac = TrichTenSach(goc);
        if (tenTrongNgoac != null
            || ChuaBatKy(chuan, "con khong", "con hang khong", "het chua", "con hay het", "con sach khong", "muon duoc khong"))
            return new KetQuaNhanDien(ChatIntent.SachConHayHet, tenTrongNgoac);

        // 10) Hạn trả khi nào.
        if (Chua(chuan, "han tra") || Chua(chuan, "khi nao tra") || (Chua(chuan, "khi nao") && Chua(chuan, "tra")))
            return new KetQuaNhanDien(ChatIntent.HanTraKhiNao, null);

        // 11) Đang mượn những sách nào.
        if (Chua(chuan, "dang muon")
            || (Chua(chuan, "muon") && ChuaBatKy(chuan, "sach nao", "sach gi", "nhung sach", "quyen nao")))
            return new KetQuaNhanDien(ChatIntent.DangMuonSachGi, null);

        // 12) Lời chào — chỉ nhận diện khi câu khá ngắn, tránh nhầm với câu hỏi thật có chứa từ tương tự.
        if (chuan.Length <= 25)
        {
            var tuRieng = chuan.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            string[] loiChao = { "chao", "hello", "hi", "alo" };
            if (tuRieng.Any(tu => loiChao.Contains(tu)))
                return new KetQuaNhanDien(ChatIntent.Chao, null);
        }

        return new KetQuaNhanDien(ChatIntent.NgoaiPhamVi, null);
    }

    /// <summary>Trích tên sách nằm trong dấu ngoặc kép (thẳng " " hoặc cong “ ”) hoặc nháy đơn ' ', nếu có.</summary>
    private static string? TrichTenSach(string text)
    {
        var m = Regex.Match(text, "[\"“]([^\"”]+)[\"”]");
        if (m.Success) return m.Groups[1].Value.Trim();

        m = Regex.Match(text, "'([^']+)'");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static bool Chua(string chuan, string tuKhoa) => chuan.Contains(tuKhoa);

    private static bool ChuaBatKy(string chuan, params string[] tuKhoas) => tuKhoas.Any(t => chuan.Contains(t));
}
