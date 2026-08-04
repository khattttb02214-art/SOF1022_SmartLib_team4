using System.ComponentModel.DataAnnotations;
using SmartLib.Web.Attributes;

namespace SmartLib.Web.ViewModels;

public class BookViewModel
{
    // Không [Required] – sẽ được auto-gen trong controller
    [StringLength(10)]
    public string? MaSach { get; set; }

    [Required(ErrorMessage = "Tên sách không được trống")]
    [StringLength(200)]
    public string TenSach { get; set; } = null!;

    [Required(ErrorMessage = "ISBN không được trống")]
    [StringLength(20)]
    public string? ISBN { get; set; }

    // Không [Required] — nếu để trống, controller sẽ tự lấy Barcode = Mã sách
    [StringLength(100)]
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "Thể loại không được trống")]
    public string? MaTheLoai { get; set; }

    [Required(ErrorMessage = "Nhà xuất bản không được trống")]
    public string? MaNXB { get; set; }

    [Required(ErrorMessage = "Kệ sách không được trống")]
    public string? MaKe { get; set; }

    [Required(ErrorMessage = "Năm xuất bản không được trống")]
    [Range(1800, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    public int? NamXuatBan { get; set; }

    [Required(ErrorMessage = "Ngôn ngữ không được trống")]
    public string? NgonNgu { get; set; }

    [Required(ErrorMessage = "Số trang không được trống")]
    [Range(1, 10000, ErrorMessage = "Số trang phải lớn hơn 0")]
    public int? SoTrang { get; set; }

    // Không bắt buộc — nhiều sách (đặc biệt sách cũ/nhập nhanh) chưa có sẵn tóm tắt,
    // để trống cũng lưu được, mô tả có thể bổ sung sau.
    public string? MoTa { get; set; }

    [Range(1, 10000, ErrorMessage = "Số lượng kho phải ít nhất là 1")]
    public int SoLuongKho { get; set; }

    // Cho phép nhập tay khi TẠO MỚI (mặc định JS tự điền = SoLuongKho, admin vẫn có
    // thể sửa thấp hơn nếu cần). [NhoHonHoacBang] đảm bảo KHÔNG BAO GIỜ được nhập lớn
    // hơn SoLuongKho (chặn cả 2 lớp: JS báo ngay lúc gõ + ModelState chặn lại lần nữa
    // khi submit, phòng trường hợp bypass JS). Khi SỬA, view vẫn khóa readonly như cũ
    // (giữ nguyên giá trị thực tế tính từ kho) — xem ghi chú trong BooksController.Edit.
    [Range(0, 10000, ErrorMessage = "Số lượng khả dụng không hợp lệ")]
    [NhoHonHoacBang("SoLuongKho", ErrorMessage = "Số lượng khả dụng phải nhỏ hơn hoặc bằng Số lượng kho")]
    public int SoLuongKhaDung { get; set; }

    public string? AnhBia { get; set; }

    // Bắt buộc khi TẠO MỚI (kiểm tra thủ công trong action Create, vì Edit dùng
    // chung ViewModel này nhưng không bắt buộc chọn lại ảnh mỗi lần sửa).
    public IFormFile? AnhBiaFile { get; set; }

    public List<string> SelectedTacGias { get; set; } = new();
}

/// <summary>Kết quả sau khi nhập ảnh bìa hàng loạt (khớp file ảnh với sách theo Mã sách/ISBN trong tên file).</summary>
public class BulkAnhBiaKetQuaViewModel
{
    public List<BulkAnhBiaMatchedItem> DaKhop { get; set; } = new();
    public List<string> KhongKhop { get; set; } = new();
}

public class BulkAnhBiaMatchedItem
{
    public string TenFile { get; set; } = "";
    public string MaSach { get; set; } = "";
    public string TenSach { get; set; } = "";
    /// <summary>"Mã sách" hoặc "ISBN" — khớp theo tiêu chí nào.</summary>
    public string KhopTheo { get; set; } = "";
}