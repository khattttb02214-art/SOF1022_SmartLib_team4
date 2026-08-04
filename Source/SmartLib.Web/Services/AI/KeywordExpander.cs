using SmartLib.Web.Interfaces;

namespace SmartLib.Web.Services.AI;

/// <summary>
/// Cài đặt IKeywordExpander bằng từ điển "chủ đề → từ khóa mở rộng" (rule-based), so khớp trên
/// văn bản đã chuẩn hóa (bỏ dấu + viết thường) — cùng triết lý với IntentRecognizer: không cần
/// gọi API AI/LLM trả phí bên ngoài, luôn chạy được, kết quả xác định (dễ giải thích khi bảo vệ
/// đồ án), và xử lý tốt việc gõ tiếng Việt không dấu.
///
/// Nếu chủ đề không khớp mục nào trong từ điển, sẽ tự động rơi về bước trích từ khóa dự phòng
/// (loại bỏ các từ đệm thường gặp như "tôi muốn", "học", "có sách"... giữ lại phần còn lại).
///
/// Muốn nâng cấp lên mô hình AI/NLP thông minh hơn (hoặc gọi LLM ngoài) sau này: chỉ cần viết 1
/// class khác cài đặt IKeywordExpander rồi đổi đăng ký DI trong Program.cs.
/// </summary>
public class KeywordExpander : IKeywordExpander
{
    // Từ điển chủ đề: các từ khóa KÍCH HOẠT (đã viết bằng chữ thường, không dấu) → danh sách từ
    // khóa TÌM KIẾM MỞ RỘNG thực tế (giữ nguyên dấu/hoa-thường vì đây là phần dùng để so khớp
    // TÊN SÁCH thật trong Database — sách CNTT đa số đặt tên tiếng Anh).
    //
    // LƯU Ý THỨ TỰ: các chủ đề có từ kích hoạt DÀI/CỤ THỂ hơn phải đứng TRƯỚC các chủ đề có từ
    // kích hoạt NGẮN hơn dễ bị trùng khớp nhầm (VD: "javascript" phải kiểm tra trước "java", vì
    // "java" là 1 chuỗi con nằm trong "javascript"). Chủ đề khớp ĐẦU TIÊN sẽ được dùng.
    private static readonly List<(string[] KichHoat, string[] TuKhoa)> ChuDe = new()
    {
        (new[] { "c#", "csharp", "c sharp" },
            new[] { "C#", "ASP.NET", ".NET", "Entity Framework" }),

        (new[] { "javascript", "lap trinh web", "frontend", "front-end" },
            new[] { "JavaScript", "HTML", "CSS", "React" }),

        (new[] { "java" }, // kiểm tra SAU "javascript" để tránh khớp nhầm (java là chuỗi con của javascript)
            new[] { "Java", "Spring", "Android" }),

        (new[] { "python" },
            new[] { "Python", "Django", "Flask" }),

        (new[] { "tri tue nhan tao", "artificial intelligence", "machine learning", "may hoc", "hoc may", "deep learning" },
            new[] { "Artificial Intelligence", "Machine Learning", "Deep Learning" }),

        (new[] { "sql", "co so du lieu", "database", "csdl" },
            new[] { "SQL Server", "MySQL", "Database Design" }),

        (new[] { "cau truc du lieu", "giai thuat", "thuat toan", "algorithm", "data structure" },
            new[] { "Data Structures", "Algorithms", "Thuật Toán" }),

        (new[] { "mang may tinh", "networking", "he thong mang", "computer network" },
            new[] { "Computer Network", "TCP/IP", "Networking" }),

        (new[] { "he dieu hanh", "operating system" },
            new[] { "Operating System", "Linux", "Windows Server" }),

        (new[] { "bao mat", "an ninh mang", "cybersecurity", "ma hoa", "cryptography" },
            new[] { "Security", "Cybersecurity", "Cryptography" }),

        (new[] { "clean code", "ky thuat phan mem", "software engineering", "design pattern", "kien truc phan mem" },
            new[] { "Clean Code", "Software Engineering", "Design Patterns" }),

        (new[] { "quan ly du an", "agile", "scrum" },
            new[] { "Quản Lý Dự Án", "Agile", "Scrum" }),

        (new[] { "kinh te", "quan tri kinh doanh", "marketing" },
            new[] { "Kinh Tế", "Quản Trị Kinh Doanh", "Marketing" }),

        (new[] { "tieng anh", "ngu phap tieng anh", "english grammar" },
            new[] { "Tiếng Anh", "English Grammar", "Ngữ Pháp" }),

        (new[] { "van hoc", "tieu thuyet", "van chuong" },
            new[] { "Văn Học", "Tiểu Thuyết" }),

        (new[] { "toan hoc", "giai tich", "xac suat thong ke", "dai so" },
            new[] { "Toán Học", "Giải Tích", "Xác Suất Thống Kê" }),
    };

    // Các từ/cụm từ đệm phổ biến, KHÔNG mang nghĩa "chủ đề cần tìm" — loại bỏ khi trích từ khóa
    // dự phòng (trường hợp câu hỏi không khớp chủ đề nào đã biết trong từ điển ở trên).
    private static readonly string[] TuDungLoaiBo =
    {
        "toi", "muon", "can", "hoc", "tim", "kiem", "sach", "co", "khong", "ve", "cuon",
        "mot", "nao", "gi", "the", "la", "cho", "hoi", "xin", "hay", "va", "voi", "cua",
        "dang", "duoc", "quyen", "nhung", "day", "a", "nhe", "giup"
    };

    public List<string> MoRongTuKhoa(string cauHoi)
    {
        if (string.IsNullOrWhiteSpace(cauHoi)) return new List<string>();

        var chuan = VietnameseTextHelper.BoDauVaThuongHoa(cauHoi);

        foreach (var (kichHoat, tuKhoa) in ChuDe)
        {
            if (kichHoat.Any(k => chuan.Contains(k)))
                return tuKhoa.ToList();
        }

        // Không khớp chủ đề nào đã biết → trích từ khóa dự phòng bằng cách loại bỏ các từ đệm
        // thường gặp, giữ lại phần còn lại làm từ khóa tìm trực tiếp theo tên sách/tác giả/thể loại.
        return TrichTuKhoaDuPhong(cauHoi);
    }

    private static List<string> TrichTuKhoaDuPhong(string cauHoiGoc)
    {
        var tuTrongCau = cauHoiGoc.Split(
            new[] { ' ', ',', '.', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);

        var conLai = tuTrongCau
            .Where(tu => !TuDungLoaiBo.Contains(VietnameseTextHelper.BoDauVaThuongHoa(tu)))
            .ToList();

        // Nếu loại bỏ hết (câu hỏi toàn từ đệm) thì đành dùng lại nguyên câu gốc, còn hơn không có
        // từ khóa nào để tìm.
        if (conLai.Count == 0) return new List<string> { cauHoiGoc.Trim() };

        return new List<string> { string.Join(" ", conLai) };
    }
}
