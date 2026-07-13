using SmartLib.Web.Models;

namespace SmartLib.Web.ViewModels;

/// <summary>Dữ liệu cho trang Index màn hình Phân quyền.</summary>
public class PhanQuyenIndexViewModel
{
    public List<NhanVien> DanhSachNhanVien { get; set; } = new();
    public string? MaNVDangChon { get; set; }
    public bool LaAdmin { get; set; }
    public List<NhomChucNangDto> NhomChucNangs { get; set; } = new();
}

/// <summary>1 nhóm chức năng kèm danh sách chức năng con và trạng thái quyền hiện tại.</summary>
public class NhomChucNangDto
{
    public int MaNhom { get; set; }
    public string TenNhom { get; set; } = null!;
    public string? Icon { get; set; }
    public List<ChucNangQuyenDto> ChucNangs { get; set; } = new();
}

/// <summary>1 dòng chức năng trong bảng ma trận, kèm 4 quyền độc lập.</summary>
public class ChucNangQuyenDto
{
    public int MaChucNang { get; set; }
    public string TenChucNang { get; set; } = null!;
    public string? Icon { get; set; }
    public bool Xem { get; set; }
    public bool Them { get; set; }
    public bool Sua { get; set; }
    public bool Xoa { get; set; }
}

/// <summary>Payload gửi lên khi bấm Lưu.</summary>
public class PhanQuyenSaveModel
{
    public string MaNV { get; set; } = null!;
    public bool LaAdmin { get; set; }
    public List<ChucNangQuyenSaveDto> Quyens { get; set; } = new();
}

public class ChucNangQuyenSaveDto
{
    public int MaChucNang { get; set; }
    public bool Xem { get; set; }
    public bool Them { get; set; }
    public bool Sua { get; set; }
    public bool Xoa { get; set; }
}
