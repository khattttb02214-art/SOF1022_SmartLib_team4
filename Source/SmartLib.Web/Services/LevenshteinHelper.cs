namespace SmartLib.Web.Services;

/// <summary>
/// Thuật toán Levenshtein distance (khoảng cách chỉnh sửa) — dùng để tìm chuỗi GẦN GIỐNG nhất
/// khi người dùng gõ sai chính tả (VD: "Clen Code" vẫn tìm ra "Clean Code"). Đây là thuật toán
/// quy hoạch động kinh điển, không phụ thuộc thư viện ngoài, độ phức tạp O(n*m).
/// </summary>
public static class LevenshteinHelper
{
    /// <summary>
    /// Số lượt thêm/xóa/thay ký tự tối thiểu để biến chuỗi <paramref name="a"/> thành
    /// <paramref name="b"/>. Khoảng cách càng NHỎ nghĩa là 2 chuỗi càng GIỐNG nhau.
    /// </summary>
    public static int TinhKhoangCach(string? a, string? b)
    {
        a ??= string.Empty;
        b ??= string.Empty;

        int n = a.Length, m = b.Length;
        if (n == 0) return m;
        if (m == 0) return n;

        var dp = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) dp[i, 0] = i;
        for (int j = 0; j <= m; j++) dp[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int chiPhiThayThe = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1,       // xóa 1 ký tự khỏi a
                             dp[i, j - 1] + 1),       // thêm 1 ký tự vào a
                    dp[i - 1, j - 1] + chiPhiThayThe  // giữ nguyên hoặc thay thế 1 ký tự
                );
            }
        }

        return dp[n, m];
    }
}
