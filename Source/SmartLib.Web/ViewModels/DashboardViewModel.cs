namespace SmartLib.Web.ViewModels;

public class DashboardViewModel
{
    public int TongSach { get; set; }

    public int TongDocGia { get; set; }

    public int TongNhanVien { get; set; }

    public int SachDangMuon { get; set; }

    public int SachQuaHan { get; set; }

    public int TongPhieuMuon { get; set; }

    public decimal TongTienPhat { get; set; }

    public List<string> Labels { get; set; }
        = new();

    public List<int> Data { get; set; }
        = new();
}