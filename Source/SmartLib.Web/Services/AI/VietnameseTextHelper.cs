using System.Globalization;
using System.Text;

namespace SmartLib.Web.Services.AI;

/// <summary>
/// Tiện ích chuẩn hóa văn bản tiếng Việt, dùng chung cho mọi thành phần "AI" (IntentRecognizer,
/// KeywordExpander...) cần so khớp từ khóa không phân biệt dấu/hoa-thường — người dùng Việt Nam
/// khi gõ nhanh (chat, ô tìm kiếm) thường KHÔNG bật dấu tiếng Việt.
/// </summary>
public static class VietnameseTextHelper
{
    /// <summary>
    /// Bỏ dấu tiếng Việt + chuyển thường. VD: "Trí Tuệ Nhân Tạo" → "tri tue nhan tao".
    /// </summary>
    public static string BoDauVaThuongHoa(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var thuong = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in thuong)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        // "đ" không tách dấu qua NormalizationForm.FormD (nó là 1 chữ cái riêng, không phải
        // tổ hợp ký tự + dấu) nên phải thay thủ công.
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd');
    }
}
