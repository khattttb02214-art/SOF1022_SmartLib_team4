using SmartLib.Web.Models;

namespace SmartLib.Web.ViewModels;

public class WishlistViewModel
{
    // Sách đã yêu thích
    public List<Wishlist> DanhSachYeuThich { get; set; } = new();

    // Sách được gợi ý (theo sở thích)
    public List<SachGoiY> SachGoiY { get; set; } = new();

    // Thông báo sách mới chưa đọc
    public List<ThongBao> ThongBaoMoi { get; set; } = new();

    // Sở thích hiện tại
    public List<WishlistPreference> SoThich { get; set; } = new();

    // Folder đang lọc
    public List<WishlistFolder> Folders { get; set; } = new();
    public int? FolderIdDangChon { get; set; }

    // Dữ liệu cho form thêm sở thích
    public List<TheLoai> DanhSachTheLoai { get; set; } = new();
    public List<TacGia> DanhSachTacGia { get; set; } = new();
    public List<NhaXuatBan> DanhSachNXB { get; set; } = new();
}

public class SachGoiY
{
    public Sach Sach { get; set; } = null!;
    public string LyDo { get; set; } = "";      // "Cùng thể loại", "Cùng tác giả"…
    public string LoaiGoiY { get; set; } = "";  // "THELOAI" | "TACGIA" | "NXB"
    public bool DaYeuThich { get; set; }
}
