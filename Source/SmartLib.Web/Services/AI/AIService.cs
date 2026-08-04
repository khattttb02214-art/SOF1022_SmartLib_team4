using SmartLib.Web.Interfaces;

namespace SmartLib.Web.Services.AI;

/// <summary>
/// "Bộ não" điều phối của AI Assistant (Chatbot thư viện SmartLib).
///
/// Luồng xử lý đúng theo kiến trúc yêu cầu:
///   Controller → <b>AIService</b> → ISachService / IMuonTraService / IReservationService → Database.
///
/// AIService nhận câu hỏi tự nhiên, nhờ IIntentRecognizer xác định ý định, rồi LẤY DỮ LIỆU THẬT
/// của hệ thống thông qua 3 Service nói trên để soạn câu trả lời — không bao giờ tự ý truy vấn
/// DbContext hay bất kỳ bảng dữ liệu nào một cách trực tiếp.
/// </summary>
public class AIService : IAIService
{
    private readonly IIntentRecognizer _nhanDien;
    private readonly ISachService _sachService;
    private readonly IMuonTraService _muonTraService;
    private readonly IReservationService _reservationService;

    // Các con số quy định mượn trả dưới đây PHẢI khớp với logic thật đang chạy trong
    // Controllers/BorrowController.cs (thời hạn mượn 14 ngày, gia hạn +7 ngày/1 lần,
    // phạt 5.000đ/ngày trễ). Nếu sau này thay đổi quy định ở BorrowController, nhớ cập nhật
    // lại 3 hằng số dưới đây để AI luôn trả lời đúng.
    private const int SoNgayMuonMacDinh = 14;
    private const int SoNgayGiaHan = 7;
    private const int TienPhatMoiNgay = 5000;

    private const string ThongBaoNgoaiPhamVi = "Xin lỗi, tôi chỉ hỗ trợ các vấn đề liên quan đến thư viện SmartLib.";
    private const string ThongBaoKhongPhaiDocGia = "Tài khoản của bạn không phải là tài khoản độc giả nên không có dữ liệu mượn/trả sách.";

    public AIService(
        IIntentRecognizer nhanDien,
        ISachService sachService,
        IMuonTraService muonTraService,
        IReservationService reservationService)
    {
        _nhanDien = nhanDien;
        _sachService = sachService;
        _muonTraService = muonTraService;
        _reservationService = reservationService;
    }

    public async Task<string> TraLoiAsync(string cauHoi, NguoiDungHienTai nguoiDung)
    {
        if (string.IsNullOrWhiteSpace(cauHoi))
            return "Bạn muốn hỏi gì về thư viện SmartLib nè? 😊";

        var ketQua = _nhanDien.NhanDien(cauHoi);

        // Các ý định chỉ dành cho Thủ thư/Admin — chặn sớm nếu Sinh viên hỏi nhầm sang nhóm này,
        // tránh lộ số liệu thống kê nội bộ cho tài khoản không có thẩm quyền xem.
        bool laYDinhThuThu = ketQua.YDinh is ChatIntent.SoLuotMuonHomNay
            or ChatIntent.SoSachDangQuaHan
            or ChatIntent.SachDuocMuonNhieuNhat
            or ChatIntent.SoSachDangDatTruoc;

        if (laYDinhThuThu && !(nguoiDung.LaThuThu || nguoiDung.LaAdmin))
            return "Xin lỗi, số liệu thống kê này chỉ dành cho thủ thư/quản trị viên thư viện.";

        return ketQua.YDinh switch
        {
            ChatIntent.Chao => TraLoiChao(nguoiDung),
            ChatIntent.DangMuonSachGi => await TraLoiDangMuonSachGiAsync(nguoiDung),
            ChatIntent.HanTraKhiNao => await TraLoiHanTraKhiNaoAsync(nguoiDung),
            ChatIntent.DaQuaHanChua => await TraLoiDaQuaHanChuaAsync(nguoiDung),
            ChatIntent.DaDatTruocSachGi => await TraLoiDaDatTruocSachGiAsync(nguoiDung),
            ChatIntent.SachConHayHet => await TraLoiSachConHayHetAsync(ketQua.ThamSo),
            ChatIntent.CoTheGiaHanKhong => await TraLoiCoTheGiaHanKhongAsync(nguoiDung),
            ChatIntent.QuyDinhMuonTra => TraLoiQuyDinhMuonTra(),
            ChatIntent.SoLuotMuonHomNay => await TraLoiSoLuotMuonHomNayAsync(),
            ChatIntent.SoSachDangQuaHan => await TraLoiSoSachDangQuaHanAsync(),
            ChatIntent.SachDuocMuonNhieuNhat => await TraLoiSachDuocMuonNhieuNhatAsync(),
            ChatIntent.SoSachDangDatTruoc => await TraLoiSoSachDangDatTruocAsync(),
            _ => ThongBaoNgoaiPhamVi
        };
    }

    // ══════════════════════════ DÀNH CHO SINH VIÊN ══════════════════════════

    private static string TraLoiChao(NguoiDungHienTai nguoiDung)
    {
        return $"Chào {nguoiDung.HoTen}! 📚 Mình là trợ lý AI của thư viện SmartLib. " +
               "Bạn có thể hỏi mình về sách đang mượn, hạn trả, quá hạn, đặt trước, tình trạng " +
               "1 cuốn sách cụ thể, hoặc quy định mượn trả của thư viện nhé!";
    }

    private async Task<string> TraLoiDangMuonSachGiAsync(NguoiDungHienTai nguoiDung)
    {
        if (string.IsNullOrEmpty(nguoiDung.MaDocGia)) return ThongBaoKhongPhaiDocGia;

        var dsPhieu = await _muonTraService.LayPhieuDangMuonAsync(nguoiDung.MaDocGia);
        if (dsPhieu.Count == 0) return "Bạn hiện không mượn cuốn sách nào cả. 📖";

        var dong = dsPhieu.SelectMany(p => p.ChiTietMuonTras.Select(ct =>
            $"- {ct.Sach?.TenSach ?? "(không rõ tên sách)"} (hạn trả: {p.NgayHenTra:dd/MM/yyyy})"));

        int tongSoCuon = dsPhieu.Sum(p => p.ChiTietMuonTras.Count);
        return $"Bạn đang mượn {tongSoCuon} cuốn sách:\n{string.Join("\n", dong)}";
    }

    private async Task<string> TraLoiHanTraKhiNaoAsync(NguoiDungHienTai nguoiDung)
    {
        if (string.IsNullOrEmpty(nguoiDung.MaDocGia)) return ThongBaoKhongPhaiDocGia;

        var dsPhieu = await _muonTraService.LayPhieuDangMuonAsync(nguoiDung.MaDocGia);
        if (dsPhieu.Count == 0) return "Bạn hiện không mượn cuốn sách nào nên không có hạn trả nào cả. 📖";

        var dong = dsPhieu.SelectMany(p => p.ChiTietMuonTras.Select(ct =>
            $"- {ct.Sach?.TenSach ?? "(không rõ tên sách)"}: hạn trả {p.NgayHenTra:dd/MM/yyyy}"));

        return $"Hạn trả các sách bạn đang mượn:\n{string.Join("\n", dong)}";
    }

    private async Task<string> TraLoiDaQuaHanChuaAsync(NguoiDungHienTai nguoiDung)
    {
        if (string.IsNullOrEmpty(nguoiDung.MaDocGia)) return ThongBaoKhongPhaiDocGia;

        var dsPhieu = await _muonTraService.LayPhieuQuaHanAsync(nguoiDung.MaDocGia);
        if (dsPhieu.Count == 0) return "Bạn chưa quá hạn cuốn sách nào cả, yên tâm nhé! ✅";

        var dong = dsPhieu.SelectMany(p => p.ChiTietMuonTras.Select(ct =>
        {
            int soNgayTre = (int)(DateTime.Now.Date - p.NgayHenTra.Date).TotalDays;
            return $"- {ct.Sach?.TenSach ?? "(không rõ tên sách)"}: trễ {soNgayTre} ngày (hạn {p.NgayHenTra:dd/MM/yyyy}), phạt tạm tính {soNgayTre * TienPhatMoiNgay:N0}đ";
        }));

        return $"⚠️ Bạn đang quá hạn {dsPhieu.Count} phiếu mượn:\n{string.Join("\n", dong)}\n" +
               "Bạn nên mang sách đến trả sớm để không phát sinh thêm tiền phạt nhé.";
    }

    private async Task<string> TraLoiDaDatTruocSachGiAsync(NguoiDungHienTai nguoiDung)
    {
        if (string.IsNullOrEmpty(nguoiDung.MaDocGia)) return ThongBaoKhongPhaiDocGia;

        var dsDat = await _reservationService.LayDatTruocDangHoatDongAsync(nguoiDung.MaDocGia);
        if (dsDat.Count == 0) return "Bạn hiện không đặt trước cuốn sách nào cả.";

        var dong = dsDat.SelectMany(r => r.ChiTietDatTruocs.Select(ct =>
            $"- {ct.Sach?.TenSach ?? "(không rõ tên sách)"} (trạng thái: {r.TrangThai}, ngày đặt: {r.NgayDat:dd/MM/yyyy})"));

        return $"Bạn đang đặt trước những sách sau:\n{string.Join("\n", dong)}";
    }

    private async Task<string> TraLoiSachConHayHetAsync(string? tenSach)
    {
        if (string.IsNullOrWhiteSpace(tenSach))
            return "Bạn muốn hỏi về cuốn sách nào? Bạn cho mình xin tên sách nhé, VD: Sách \"Đắc Nhân Tâm\" còn không?";

        var ds = await _sachService.TimKiemTheoTenAsync(tenSach, 5);
        if (ds.Count == 0)
            return $"Mình không tìm thấy sách nào có tên gần giống \"{tenSach}\" trong thư viện.";

        if (ds.Count == 1)
        {
            var s = ds[0];
            return s.SoLuongKhaDung > 0
                ? $"📗 Sách \"{s.TenSach}\" hiện còn {s.SoLuongKhaDung}/{s.SoLuongKho} bản có sẵn để mượn."
                : $"📕 Sách \"{s.TenSach}\" hiện đã hết bản khả dụng (0/{s.SoLuongKho}). Bạn có thể đặt trước để được ưu tiên khi có sách trả về.";
        }

        var dong = ds.Select(s => $"- {s.TenSach}: {(s.SoLuongKhaDung > 0 ? $"còn {s.SoLuongKhaDung}/{s.SoLuongKho}" : "đã hết")}");
        return $"Mình tìm thấy {ds.Count} sách khớp với \"{tenSach}\", bạn xem có đúng ý không nhé:\n{string.Join("\n", dong)}";
    }

    private async Task<string> TraLoiCoTheGiaHanKhongAsync(NguoiDungHienTai nguoiDung)
    {
        string chinhSach = $"Mỗi phiếu mượn được gia hạn tối đa 1 lần, mỗi lần gia hạn thêm {SoNgayGiaHan} ngày. " +
                            "Việc gia hạn hiện cần thủ thư xử lý giúp bạn tại quầy thư viện, hệ thống chưa hỗ trợ tự gia hạn online.";

        if (string.IsNullOrEmpty(nguoiDung.MaDocGia)) return chinhSach;

        var dangMuon = await _muonTraService.LayPhieuDangMuonAsync(nguoiDung.MaDocGia);
        if (dangMuon.Count == 0)
            return chinhSach + "\nHiện bạn không mượn cuốn sách nào nên chưa cần gia hạn.";

        var dong = dangMuon.Select(p =>
        {
            var tenCacSach = string.Join(", ", p.ChiTietMuonTras.Select(ct => ct.Sach?.TenSach ?? "?"));
            string tinhTrang = p.DaGiaHan ? "đã gia hạn rồi, không thể gia hạn thêm" : "vẫn còn có thể gia hạn";
            return $"- Phiếu {p.MaPhieu} ({tenCacSach}): {tinhTrang}";
        });

        return $"{chinhSach}\n\nTình trạng gia hạn các phiếu bạn đang mượn:\n{string.Join("\n", dong)}";
    }

    private string TraLoiQuyDinhMuonTra()
    {
        return "📋 Quy định mượn trả của thư viện SmartLib:\n" +
               $"- Thời hạn mượn: {SoNgayMuonMacDinh} ngày kể từ ngày mượn.\n" +
               $"- Gia hạn: được gia hạn 1 lần, mỗi lần +{SoNgayGiaHan} ngày (thực hiện tại quầy thư viện).\n" +
               $"- Trả trễ hạn: phạt {TienPhatMoiNgay:N0}đ/ngày trễ.\n" +
               "- Đặt trước: có thể đặt trước sách đang hết, thủ thư sẽ duyệt và thông báo khi có sách.\n" +
               "Nếu cần biết thêm chi tiết, bạn nên liên hệ trực tiếp thủ thư nhé.";
    }

    // ══════════════════════════ DÀNH CHO THỦ THƯ / ADMIN ══════════════════════════

    private async Task<string> TraLoiSoLuotMuonHomNayAsync()
    {
        int soLuot = await _muonTraService.DemLuotMuonHomNayAsync();
        return $"📊 Hôm nay ({DateTime.Today:dd/MM/yyyy}) có {soLuot} lượt mượn được lập.";
    }

    private async Task<string> TraLoiSoSachDangQuaHanAsync()
    {
        int soLuong = await _muonTraService.DemPhieuQuaHanAsync();
        return soLuong == 0
            ? "✅ Hiện không có phiếu mượn nào bị quá hạn."
            : $"⚠️ Hiện có {soLuong} phiếu mượn đang quá hạn trả.";
    }

    private async Task<string> TraLoiSachDuocMuonNhieuNhatAsync()
    {
        var top = await _muonTraService.LaySachMuonNhieuNhatAsync();
        return top == null
            ? "Chưa có dữ liệu mượn sách nào để thống kê."
            : $"🏆 Sách được mượn nhiều nhất là \"{top.TenSach}\" với {top.TongLuotMuon} lượt mượn.";
    }

    private async Task<string> TraLoiSoSachDangDatTruocAsync()
    {
        int soLuong = await _reservationService.DemDatTruocDangHoatDongAsync();
        return $"📌 Hiện có {soLuong} đặt trước đang hoạt động (chưa hủy, chưa chuyển thành phiếu mượn).";
    }
}
