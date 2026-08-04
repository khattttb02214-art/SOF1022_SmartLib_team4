namespace SmartLib.Web.ViewModels;

/// <summary>Dữ liệu gửi lên từ JS khi người dùng gửi 1 câu hỏi cho AI Assistant.</summary>
public class ChatRequest
{
    public string Message { get; set; } = "";
}

/// <summary>1 tin nhắn trong lịch sử chat — được lưu tạm trong Session suốt phiên đăng nhập.</summary>
public class ChatMessageVM
{
    /// <summary>"user" hoặc "ai".</summary>
    public string Role { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public DateTime ThoiGian { get; set; }
}
