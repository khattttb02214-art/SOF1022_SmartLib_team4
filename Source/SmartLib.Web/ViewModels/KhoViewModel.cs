namespace SmartLib.Web.ViewModels;

public class KhoViewModel
{
    public string MaSach { get; set; } = null!;
    public string TenSach { get; set; } = null!;
    public string? AnhBia { get; set; }
    public string? TenTheLoai { get; set; }
    public int SoLuongKho { get; set; }
    public int SoLuongKhaDung { get; set; }
    public int DangMuon { get; set; }
    public string TrangThai { get; set; } = null!;
}
