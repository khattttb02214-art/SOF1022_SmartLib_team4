using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartLib.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChucVu",
                columns: table => new
                {
                    MaChucVu = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenChucVu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucVu", x => x.MaChucVu);
                });

            migrationBuilder.CreateTable(
                name: "DocGia",
                columns: table => new
                {
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Lop = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Khoa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTaoThe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThaiThe = table.Column<bool>(type: "bit", nullable: false),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaTheTV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DaDuyet = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocGia", x => x.MaDocGia);
                });

            migrationBuilder.CreateTable(
                name: "GoogleOtpTemp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GoogleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    OtpExpiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleOtpTemp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhaXuatBan",
                columns: table => new
                {
                    MaNXB = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenNXB = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaXuatBan", x => x.MaNXB);
                });

            migrationBuilder.CreateTable(
                name: "NhomChucNang",
                columns: table => new
                {
                    MaNhom = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNhom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhomChucNang", x => x.MaNhom);
                });

            migrationBuilder.CreateTable(
                name: "TacGia",
                columns: table => new
                {
                    MaTacGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenTacGia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TieuSu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuocTich = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ButDanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NamSinh = table.Column<int>(type: "int", nullable: true),
                    NamMat = table.Column<int>(type: "int", nullable: true),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacGia", x => x.MaTacGia);
                });

            migrationBuilder.CreateTable(
                name: "TheLoai",
                columns: table => new
                {
                    MaTheLoai = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenTheLoai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheLoai", x => x.MaTheLoai);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    MaNV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaChucVu = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LaAdmin = table.Column<bool>(type: "bit", nullable: false),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    OtpCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtpExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    PendingEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MatKhauTuDat = table.Column<bool>(type: "bit", nullable: false),
                    OtpResetMatKhau = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    OtpResetMatKhauHetHan = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVien", x => x.MaNV);
                    table.ForeignKey(
                        name: "FK_NhanVien_ChucVu_MaChucVu",
                        column: x => x.MaChucVu,
                        principalTable: "ChucVu",
                        principalColumn: "MaChucVu");
                });

            migrationBuilder.CreateTable(
                name: "TheThuVien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AnhThe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheThuVien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheThuVien_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WishlistFolder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistFolder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistFolder_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia");
                });

            migrationBuilder.CreateTable(
                name: "WishlistPreference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LoaiSoThich = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaRef = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistPreference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistPreference_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChucNang",
                columns: table => new
                {
                    MaChucNang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhom = table.Column<int>(type: "int", nullable: false),
                    TenChucNang = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Controller = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucNang", x => x.MaChucNang);
                    table.ForeignKey(
                        name: "FK_ChucNang_NhomChucNang_MaNhom",
                        column: x => x.MaNhom,
                        principalTable: "NhomChucNang",
                        principalColumn: "MaNhom",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeSach",
                columns: table => new
                {
                    MaKe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenKe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ViTri = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tang = table.Column<int>(type: "int", nullable: true),
                    Phong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SucChua = table.Column<int>(type: "int", nullable: true),
                    MaNXBPhuTrach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaTheLoaiPhuTrach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeSach", x => x.MaKe);
                    table.ForeignKey(
                        name: "FK_KeSach_NhaXuatBan_MaNXBPhuTrach",
                        column: x => x.MaNXBPhuTrach,
                        principalTable: "NhaXuatBan",
                        principalColumn: "MaNXB",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KeSach_TheLoai_MaTheLoaiPhuTrach",
                        column: x => x.MaTheLoaiPhuTrach,
                        principalTable: "TheLoai",
                        principalColumn: "MaTheLoai",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MuonTra",
                columns: table => new
                {
                    MaPhieu = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaNV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NgayMuon = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHenTra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTraThucTe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TienPhat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DaGiaHan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MuonTra", x => x.MaPhieu);
                    table.ForeignKey(
                        name: "FK_MuonTra_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MuonTra_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHoatDong",
                columns: table => new
                {
                    MaLog = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HanhDong = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHoatDong", x => x.MaLog);
                    table.ForeignKey(
                        name: "FK_NhatKyHoatDong_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    MaReservation = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NgayDat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaNV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DaMuon = table.Column<bool>(type: "bit", nullable: false),
                    MaPhieuMuon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.MaReservation);
                    table.ForeignKey(
                        name: "FK_Reservation_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia");
                    table.ForeignKey(
                        name: "FK_Reservation_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV");
                });

            migrationBuilder.CreateTable(
                name: "PhanQuyenNhanVien",
                columns: table => new
                {
                    MaPQ = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MaChucNang = table.Column<int>(type: "int", nullable: false),
                    DuocXem = table.Column<bool>(type: "bit", nullable: false),
                    DuocThem = table.Column<bool>(type: "bit", nullable: false),
                    DuocSua = table.Column<bool>(type: "bit", nullable: false),
                    DuocXoa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanQuyenNhanVien", x => x.MaPQ);
                    table.ForeignKey(
                        name: "FK_PhanQuyenNhanVien_ChucNang_MaChucNang",
                        column: x => x.MaChucNang,
                        principalTable: "ChucNang",
                        principalColumn: "MaChucNang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhanQuyenNhanVien_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sach",
                columns: table => new
                {
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ISBN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenSach = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaTheLoai = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaNXB = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NamXuatBan = table.Column<int>(type: "int", nullable: true),
                    NgonNgu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoTrang = table.Column<int>(type: "int", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaKe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SoLuongKho = table.Column<int>(type: "int", nullable: false),
                    SoLuongKhaDung = table.Column<int>(type: "int", nullable: false),
                    AnhBia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sach", x => x.MaSach);
                    table.ForeignKey(
                        name: "FK_Sach_KeSach_MaKe",
                        column: x => x.MaKe,
                        principalTable: "KeSach",
                        principalColumn: "MaKe");
                    table.ForeignKey(
                        name: "FK_Sach_NhaXuatBan_MaNXB",
                        column: x => x.MaNXB,
                        principalTable: "NhaXuatBan",
                        principalColumn: "MaNXB");
                    table.ForeignKey(
                        name: "FK_Sach_TheLoai_MaTheLoai",
                        column: x => x.MaTheLoai,
                        principalTable: "TheLoai",
                        principalColumn: "MaTheLoai");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDatTruoc",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaReservation = table.Column<int>(type: "int", nullable: false),
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDatTruoc", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietDatTruoc_Reservation_MaReservation",
                        column: x => x.MaReservation,
                        principalTable: "Reservation",
                        principalColumn: "MaReservation",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDatTruoc_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach");
                });

            migrationBuilder.CreateTable(
                name: "CuonSach",
                columns: table => new
                {
                    MaCuonSach = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayNhap = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuonSach", x => x.MaCuonSach);
                    table.ForeignKey(
                        name: "FK_CuonSach_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach");
                });

            migrationBuilder.CreateTable(
                name: "DanhGiaSach",
                columns: table => new
                {
                    MaDanhGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaSach", x => x.MaDanhGia);
                    table.ForeignKey(
                        name: "FK_DanhGiaSach_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia");
                    table.ForeignKey(
                        name: "FK_DanhGiaSach_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach");
                });

            migrationBuilder.CreateTable(
                name: "Ebook",
                columns: table => new
                {
                    MaEbook = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TenFile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DuongDanFile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DinhDangFile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: true),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ebook", x => x.MaEbook);
                    table.ForeignKey(
                        name: "FK_Ebook_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sach_TacGia",
                columns: table => new
                {
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MaTacGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sach_TacGia", x => new { x.MaSach, x.MaTacGia });
                    table.ForeignKey(
                        name: "FK_Sach_TacGia_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sach_TacGia_TacGia_MaTacGia",
                        column: x => x.MaTacGia,
                        principalTable: "TacGia",
                        principalColumn: "MaTacGia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaNV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoaiThongBao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBao_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ThongBao_NhanVien_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanVien",
                        principalColumn: "MaNV");
                    table.ForeignKey(
                        name: "FK_ThongBao_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Wishlist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDocGia = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NgayThem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FolderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wishlist_DocGia_MaDocGia",
                        column: x => x.MaDocGia,
                        principalTable: "DocGia",
                        principalColumn: "MaDocGia");
                    table.ForeignKey(
                        name: "FK_Wishlist_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach");
                    table.ForeignKey(
                        name: "FK_Wishlist_WishlistFolder_FolderId",
                        column: x => x.FolderId,
                        principalTable: "WishlistFolder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietMuonTra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieu = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaSach = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaCuonSach = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    TienPhat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietMuonTra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietMuonTra_CuonSach_MaCuonSach",
                        column: x => x.MaCuonSach,
                        principalTable: "CuonSach",
                        principalColumn: "MaCuonSach");
                    table.ForeignKey(
                        name: "FK_ChiTietMuonTra_MuonTra_MaPhieu",
                        column: x => x.MaPhieu,
                        principalTable: "MuonTra",
                        principalColumn: "MaPhieu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietMuonTra_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatTruoc_MaReservation",
                table: "ChiTietDatTruoc",
                column: "MaReservation");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatTruoc_MaSach",
                table: "ChiTietDatTruoc",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietMuonTra_MaCuonSach",
                table: "ChiTietMuonTra",
                column: "MaCuonSach");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietMuonTra_MaPhieu",
                table: "ChiTietMuonTra",
                column: "MaPhieu");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietMuonTra_MaSach",
                table: "ChiTietMuonTra",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_ChucNang_MaNhom",
                table: "ChucNang",
                column: "MaNhom");

            migrationBuilder.CreateIndex(
                name: "IX_CuonSach_MaSach",
                table: "CuonSach",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaSach_MaDocGia",
                table: "DanhGiaSach",
                column: "MaDocGia");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaSach_MaSach",
                table: "DanhGiaSach",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_Ebook_MaSach",
                table: "Ebook",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_KeSach_MaNXBPhuTrach",
                table: "KeSach",
                column: "MaNXBPhuTrach");

            migrationBuilder.CreateIndex(
                name: "IX_KeSach_MaTheLoaiPhuTrach",
                table: "KeSach",
                column: "MaTheLoaiPhuTrach");

            migrationBuilder.CreateIndex(
                name: "IX_MuonTra_MaDocGia",
                table: "MuonTra",
                column: "MaDocGia");

            migrationBuilder.CreateIndex(
                name: "IX_MuonTra_MaNV",
                table: "MuonTra",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_MaChucVu",
                table: "NhanVien",
                column: "MaChucVu");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHoatDong_MaNV",
                table: "NhatKyHoatDong",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_PhanQuyenNhanVien_MaChucNang",
                table: "PhanQuyenNhanVien",
                column: "MaChucNang");

            migrationBuilder.CreateIndex(
                name: "IX_PhanQuyenNhanVien_MaNV_MaChucNang",
                table: "PhanQuyenNhanVien",
                columns: new[] { "MaNV", "MaChucNang" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_MaDocGia",
                table: "Reservation",
                column: "MaDocGia");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_MaNV",
                table: "Reservation",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_MaKe",
                table: "Sach",
                column: "MaKe");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_MaNXB",
                table: "Sach",
                column: "MaNXB");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_MaTheLoai",
                table: "Sach",
                column: "MaTheLoai");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_TacGia_MaTacGia",
                table: "Sach_TacGia",
                column: "MaTacGia");

            migrationBuilder.CreateIndex(
                name: "IX_TheThuVien_MaDocGia",
                table: "TheThuVien",
                column: "MaDocGia");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaDocGia",
                table: "ThongBao",
                column: "MaDocGia");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaNV",
                table: "ThongBao",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaSach",
                table: "ThongBao",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_FolderId",
                table: "Wishlist",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_MaDocGia",
                table: "Wishlist",
                column: "MaDocGia");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_MaSach",
                table: "Wishlist",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistFolder_MaDocGia",
                table: "WishlistFolder",
                column: "MaDocGia");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistPreference_MaDocGia",
                table: "WishlistPreference",
                column: "MaDocGia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDatTruoc");

            migrationBuilder.DropTable(
                name: "ChiTietMuonTra");

            migrationBuilder.DropTable(
                name: "DanhGiaSach");

            migrationBuilder.DropTable(
                name: "Ebook");

            migrationBuilder.DropTable(
                name: "GoogleOtpTemp");

            migrationBuilder.DropTable(
                name: "NhatKyHoatDong");

            migrationBuilder.DropTable(
                name: "PhanQuyenNhanVien");

            migrationBuilder.DropTable(
                name: "Sach_TacGia");

            migrationBuilder.DropTable(
                name: "TheThuVien");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "Wishlist");

            migrationBuilder.DropTable(
                name: "WishlistPreference");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "CuonSach");

            migrationBuilder.DropTable(
                name: "MuonTra");

            migrationBuilder.DropTable(
                name: "ChucNang");

            migrationBuilder.DropTable(
                name: "TacGia");

            migrationBuilder.DropTable(
                name: "WishlistFolder");

            migrationBuilder.DropTable(
                name: "Sach");

            migrationBuilder.DropTable(
                name: "NhanVien");

            migrationBuilder.DropTable(
                name: "NhomChucNang");

            migrationBuilder.DropTable(
                name: "DocGia");

            migrationBuilder.DropTable(
                name: "KeSach");

            migrationBuilder.DropTable(
                name: "ChucVu");

            migrationBuilder.DropTable(
                name: "NhaXuatBan");

            migrationBuilder.DropTable(
                name: "TheLoai");
        }
    }
}
