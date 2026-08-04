namespace SmartLib.Web.Interfaces;

/// <summary>
/// Các "ý định" (intent) mà AI Assistant có thể nhận diện được từ câu hỏi tự nhiên của người dùng.
/// Mỗi giá trị ứng với đúng 1 nhóm câu hỏi trong yêu cầu nghiệp vụ của chức năng Chatbot thư viện.
/// </summary>
public enum ChatIntent
{
    /// <summary>Không nhận diện được ý định phù hợp, hoặc câu hỏi nằm ngoài phạm vi thư viện.</summary>
    NgoaiPhamVi,

    /// <summary>Lời chào / mở đầu hội thoại (VD: "chào", "hi", "xin chào").</summary>
    Chao,

    // ───────── Dành cho Sinh viên (role STU) ─────────

    /// <summary>"Tôi đang mượn những sách nào?"</summary>
    DangMuonSachGi,

    /// <summary>"Khi nào đến hạn trả?"</summary>
    HanTraKhiNao,

    /// <summary>"Tôi đã quá hạn chưa?"</summary>
    DaQuaHanChua,

    /// <summary>"Tôi đã đặt trước những sách nào?"</summary>
    DaDatTruocSachGi,

    /// <summary>"Sách 'Tên sách' còn hay đã hết?"</summary>
    SachConHayHet,

    /// <summary>"Có thể gia hạn sách không?"</summary>
    CoTheGiaHanKhong,

    /// <summary>"Quy định mượn trả như thế nào?"</summary>
    QuyDinhMuonTra,

    // ───────── Dành cho Thủ thư / Admin (role LIB, ADMIN) ─────────

    /// <summary>"Hôm nay có bao nhiêu lượt mượn?"</summary>
    SoLuotMuonHomNay,

    /// <summary>"Có bao nhiêu sách đang quá hạn?" (thống kê toàn hệ thống)</summary>
    SoSachDangQuaHan,

    /// <summary>"Sách nào được mượn nhiều nhất?"</summary>
    SachDuocMuonNhieuNhat,

    /// <summary>"Có bao nhiêu sách đang được đặt trước?" (thống kê toàn hệ thống)</summary>
    SoSachDangDatTruoc
}
