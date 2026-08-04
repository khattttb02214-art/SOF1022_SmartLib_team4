namespace SmartLib.Web.ViewModels;

public class ReservationViewModel
{
    public int MaReservation { get; set; }
    public string? MaDocGia { get; set; }
    public DateTime NgayDat { get; set; } = DateTime.Now;
    public string? TrangThai { get; set; } = "Đang Chờ";
    public string? GhiChu { get; set; }
    public List<string> SelectedBooks { get; set; } = new();
    public List<ChiTietDatTruocViewModel> ChiTietList { get; set; } = new();
}

public class ChiTietDatTruocViewModel
{
    public int MaChiTiet { get; set; }
    public string? MaSach { get; set; }
    public string? TenSach { get; set; }
    public int SoLuong { get; set; } = 1;
    public string? GhiChu { get; set; }
}
