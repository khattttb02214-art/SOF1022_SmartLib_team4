namespace SmartLib.Web.Attributes;

/// <summary>Loại quyền trong ma trận phân quyền (khớp 4 cột DuocXem/DuocThem/DuocSua/DuocXoa).</summary>
public enum LoaiQuyen { Xem, Them, Sua, Xoa }

/// <summary>
/// Gắn lên 1 action (hoặc cả controller) để CHỈ ĐỊNH TƯỜNG MINH action đó cần loại
/// quyền nào trong ma trận Phân quyền, thay vì để <see cref="SmartLib.Web.Filters.PhanQuyenActionFilter"/>
/// tự đoán qua tên action.
///
/// Dùng khi tên action không theo quy ước (Create/Edit/Delete/Xoa/Them/Sua...) hoặc
/// khi quy ước đoán sai. Ví dụ:
///
///   [ChucNangQuyen(LoaiQuyen.Xoa)]
///   public async Task&lt;IActionResult&gt; HuyBoDuLieu(string id) { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ChucNangQuyenAttribute : Attribute
{
    public LoaiQuyen Loai { get; }
    public ChucNangQuyenAttribute(LoaiQuyen loai) => Loai = loai;
}

/// <summary>
/// Gắn lên 1 action để chỉ định action đó thuộc về 1 dòng ChucNang CỤ THỂ (theo
/// MaChucNang), THAY VÌ để <see cref="SmartLib.Web.Filters.PhanQuyenActionFilter"/>
/// tự khớp theo tên Controller như mặc định.
///
/// Cần dùng khi 1 Controller có NHIỀU dòng ChucNang trỏ vào (VD: BooksController vừa
/// là "Quản lý sách" vừa là "Thể loại sách") — nếu không chỉ định, filter sẽ không
/// biết chính xác action đó phải đối chiếu với dòng ChucNang nào.
///
///   [ThuocChucNang(4)] // Thể loại sách
///   public async Task&lt;IActionResult&gt; CreateTheLoaiAjax(...) { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ThuocChucNangAttribute : Attribute
{
    public int MaChucNang { get; }
    public ThuocChucNangAttribute(int maChucNang) => MaChucNang = maChucNang;
}

/// <summary>
/// Gắn lên 1 action (hoặc cả controller) để LOẠI TRỪ HẲN khỏi việc kiểm tra phân quyền
/// chi tiết — action luôn được phép chạy (miễn đã qua [Authorize] role thông thường).
/// Dùng cho các action phụ trợ không nên bị chặn hoặc không thuộc riêng 1 chức năng
/// nào (VD: trang chủ, hoặc 1 API đếm thông báo gộp từ nhiều nguồn khác nhau).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class BoQuaPhanQuyenAttribute : Attribute { }
