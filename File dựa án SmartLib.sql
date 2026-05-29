CREATE DATABASE SmartLibDB;
GO

USE SmartLibDB;
GO

/* =========================================================
SMARTLIB OFFLINE - HỆ THỐNG QUẢN LÝ THƯ VIỆN
Chuẩn hóa hoàn chỉnh cho ASP.NET Core MVC + EF Core
========================================================= */

------------------------------------------------------------
-- 1. BẢNG CHỨC VỤ / PHÂN QUYỀN
------------------------------------------------------------

CREATE TABLE ChucVu (
    MaChucVu VARCHAR(10) PRIMARY KEY,
    TenChucVu NVARCHAR(50) NOT NULL UNIQUE
);
GO

------------------------------------------------------------
-- 2. BẢNG NHÂN VIÊN
------------------------------------------------------------

CREATE TABLE NhanVien (
    MaNV VARCHAR(10) PRIMARY KEY,

    HoTen NVARCHAR(100) NOT NULL,

    Email VARCHAR(100) UNIQUE,

    SoDienThoai VARCHAR(15),

    DiaChi NVARCHAR(255),

    MatKhau VARCHAR(255) NOT NULL,

    AnhDaiDien VARCHAR(255),

    MaChucVu VARCHAR(10),

    TrangThai BIT DEFAULT 1,

    NgayTao DATETIME DEFAULT GETDATE(),

    NgayCapNhat DATETIME,

    CONSTRAINT FK_NhanVien_ChucVu
        FOREIGN KEY (MaChucVu)
        REFERENCES ChucVu(MaChucVu)
);
GO

------------------------------------------------------------
-- 3. BẢNG ĐỘC GIẢ / SINH VIÊN
------------------------------------------------------------

CREATE TABLE DocGia (
    MaDocGia VARCHAR(10) PRIMARY KEY,

    HoTen NVARCHAR(100) NOT NULL,

    Lop NVARCHAR(50),

    Khoa NVARCHAR(100),

    Email VARCHAR(100) UNIQUE,

    SoDienThoai VARCHAR(15),

    DiaChi NVARCHAR(255),

    NgaySinh DATE,

    NgayTaoThe DATE DEFAULT GETDATE(),

    NgayHetHan DATE,

    TrangThaiThe BIT DEFAULT 1
);
GO

------------------------------------------------------------
-- 4. BẢNG THỂ LOẠI
------------------------------------------------------------

CREATE TABLE TheLoai (
    MaTheLoai VARCHAR(10) PRIMARY KEY,

    TenTheLoai NVARCHAR(100) NOT NULL UNIQUE,

    MoTa NVARCHAR(500)
);
GO

------------------------------------------------------------
-- 5. BẢNG TÁC GIẢ
------------------------------------------------------------

CREATE TABLE TacGia (
    MaTacGia VARCHAR(10) PRIMARY KEY,

    TenTacGia NVARCHAR(100) NOT NULL,

    TieuSu NVARCHAR(MAX),

    QuocTich NVARCHAR(100)
);
GO

------------------------------------------------------------
-- 6. BẢNG NHÀ XUẤT BẢN
------------------------------------------------------------

CREATE TABLE NhaXuatBan (
    MaNXB VARCHAR(10) PRIMARY KEY,

    TenNXB NVARCHAR(100) NOT NULL,

    DiaChi NVARCHAR(255),

    SoDienThoai VARCHAR(15),

    Email VARCHAR(100)
);
GO

------------------------------------------------------------
-- 7. BẢNG KỆ SÁCH
------------------------------------------------------------

CREATE TABLE KeSach (
    MaKe VARCHAR(10) PRIMARY KEY,

    TenKe NVARCHAR(50) NOT NULL,

    ViTri NVARCHAR(100)
);
GO

------------------------------------------------------------
-- 8. BẢNG SÁCH
------------------------------------------------------------

CREATE TABLE Sach (
    MaSach VARCHAR(10) PRIMARY KEY,

    ISBN VARCHAR(20) UNIQUE,

    Barcode VARCHAR(100) UNIQUE,

    TenSach NVARCHAR(200) NOT NULL,

    MaTheLoai VARCHAR(10),

    MaNXB VARCHAR(10),

    NamXuatBan INT,

    NgonNgu NVARCHAR(50),

    SoTrang INT,

    MoTa NVARCHAR(MAX),

    AnhBia VARCHAR(255),

    SoLuongKho INT DEFAULT 0,

    SoLuongKhaDung INT DEFAULT 0,

    MaKe VARCHAR(10),

    TrangThai BIT DEFAULT 1,

    NgayTao DATETIME DEFAULT GETDATE(),

    NgayCapNhat DATETIME,

    CONSTRAINT FK_Sach_TheLoai
        FOREIGN KEY (MaTheLoai)
        REFERENCES TheLoai(MaTheLoai),

    CONSTRAINT FK_Sach_NXB
        FOREIGN KEY (MaNXB)
        REFERENCES NhaXuatBan(MaNXB),

    CONSTRAINT FK_Sach_KeSach
        FOREIGN KEY (MaKe)
        REFERENCES KeSach(MaKe)
);
GO

------------------------------------------------------------
-- 9. BẢNG SÁCH - TÁC GIẢ
------------------------------------------------------------

CREATE TABLE Sach_TacGia (
    MaSach VARCHAR(10),

    MaTacGia VARCHAR(10),

    PRIMARY KEY (MaSach, MaTacGia),

    CONSTRAINT FK_SachTacGia_Sach
        FOREIGN KEY (MaSach)
        REFERENCES Sach(MaSach),

    CONSTRAINT FK_SachTacGia_TacGia
        FOREIGN KEY (MaTacGia)
        REFERENCES TacGia(MaTacGia)
);
GO

------------------------------------------------------------
-- 10. BẢNG CUỐN SÁCH
------------------------------------------------------------

CREATE TABLE CuonSach (
    MaCuonSach VARCHAR(20) PRIMARY KEY,

    MaSach VARCHAR(10),

    Barcode VARCHAR(100) UNIQUE,

    TrangThai NVARCHAR(50)
        DEFAULT N'Có Sẵn',

    NgayNhap DATE DEFAULT GETDATE(),

    CONSTRAINT FK_CuonSach_Sach
        FOREIGN KEY (MaSach)
        REFERENCES Sach(MaSach)
);
GO

------------------------------------------------------------
-- 11. BẢNG WISHLIST
------------------------------------------------------------

CREATE TABLE Wishlist (
    MaDocGia VARCHAR(10),

    MaSach VARCHAR(10),

    NgayThem DATE DEFAULT GETDATE(),

    PRIMARY KEY (MaDocGia, MaSach),

    CONSTRAINT FK_Wishlist_DocGia
        FOREIGN KEY (MaDocGia)
        REFERENCES DocGia(MaDocGia),

    CONSTRAINT FK_Wishlist_Sach
        FOREIGN KEY (MaSach)
        REFERENCES Sach(MaSach)
);
GO

------------------------------------------------------------
-- 12. BẢNG ĐÁNH GIÁ SÁCH
------------------------------------------------------------

CREATE TABLE DanhGiaSach (
    MaDanhGia INT IDENTITY(1,1) PRIMARY KEY,

    MaDocGia VARCHAR(10),

    MaSach VARCHAR(10),

    SoSao INT CHECK (SoSao >= 1 AND SoSao <= 5),

    NoiDung NVARCHAR(1000),

    NgayDanhGia DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_DanhGia_DocGia
        FOREIGN KEY (MaDocGia)
        REFERENCES DocGia(MaDocGia),

    CONSTRAINT FK_DanhGia_Sach
        FOREIGN KEY (MaSach)
        REFERENCES Sach(MaSach)
);
GO

------------------------------------------------------------
-- 13. BẢNG PHIẾU MƯỢN
------------------------------------------------------------

CREATE TABLE MuonTra (
    MaPhieu VARCHAR(10) PRIMARY KEY,

    MaDocGia VARCHAR(10),

    MaNV VARCHAR(10),

    NgayMuon DATE DEFAULT GETDATE(),

    NgayHenTra DATE NOT NULL,

    NgayTraThucTe DATE,

    TienPhat DECIMAL(18,2) DEFAULT 0,

    TrangThai NVARCHAR(50)
        DEFAULT N'Chưa Trả',

    GhiChu NVARCHAR(255),

    CONSTRAINT FK_MuonTra_DocGia
        FOREIGN KEY (MaDocGia)
        REFERENCES DocGia(MaDocGia),

    CONSTRAINT FK_MuonTra_NhanVien
        FOREIGN KEY (MaNV)
        REFERENCES NhanVien(MaNV)
);
GO

------------------------------------------------------------
-- 14. BẢNG CHI TIẾT MƯỢN TRẢ
------------------------------------------------------------

CREATE TABLE ChiTietMuonTra (
    MaPhieu VARCHAR(10),

    MaCuonSach VARCHAR(20),

    GhiChu NVARCHAR(255),

    PRIMARY KEY (MaPhieu, MaCuonSach),

    CONSTRAINT FK_CTMT_MuonTra
        FOREIGN KEY (MaPhieu)
        REFERENCES MuonTra(MaPhieu),

    CONSTRAINT FK_CTMT_CuonSach
        FOREIGN KEY (MaCuonSach)
        REFERENCES CuonSach(MaCuonSach)
);
GO

------------------------------------------------------------
-- 15. BẢNG THÔNG BÁO
------------------------------------------------------------

CREATE TABLE ThongBao (
    MaThongBao INT IDENTITY(1,1) PRIMARY KEY,

    MaDocGia VARCHAR(10),

    NoiDung NVARCHAR(500),

    DaDoc BIT DEFAULT 0,

    NgayTao DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_ThongBao_DocGia
        FOREIGN KEY (MaDocGia)
        REFERENCES DocGia(MaDocGia)
);
GO

------------------------------------------------------------
-- 16. BẢNG NHẬT KÝ HOẠT ĐỘNG
------------------------------------------------------------

CREATE TABLE NhatKyHoatDong (
    MaLog INT IDENTITY(1,1) PRIMARY KEY,

    MaNV VARCHAR(10),

    HanhDong NVARCHAR(255),

    MoTa NVARCHAR(MAX),

    ThoiGian DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_NhatKy_NhanVien
        FOREIGN KEY (MaNV)
        REFERENCES NhanVien(MaNV)
);
GO

------------------------------------------------------------
-- 17. BẢNG EBOOK
------------------------------------------------------------

CREATE TABLE Ebook (
    MaEbook INT IDENTITY(1,1) PRIMARY KEY,

    MaSach VARCHAR(10),

    TenFile NVARCHAR(255),

    DuongDanFile VARCHAR(255),

    DinhDangFile VARCHAR(20),

    KichThuoc BIGINT,

    NgayTaiLen DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Ebook_Sach
        FOREIGN KEY (MaSach)
        REFERENCES Sach(MaSach)
);
GO

------------------------------------------------------------
-- 18. BẢNG SAO LƯU DỮ LIỆU
------------------------------------------------------------

CREATE TABLE BackupDuLieu (
    MaBackup INT IDENTITY(1,1) PRIMARY KEY,

    TenFile VARCHAR(255),

    NgayBackup DATETIME DEFAULT GETDATE(),

    MaNV VARCHAR(10),

    CONSTRAINT FK_Backup_NhanVien
        FOREIGN KEY (MaNV)
        REFERENCES NhanVien(MaNV)
);
GO

------------------------------------------------------------
-- INDEX TĂNG TỐC
------------------------------------------------------------

CREATE INDEX IX_Sach_TenSach
ON Sach(TenSach);

CREATE INDEX IX_Sach_ISBN
ON Sach(ISBN);

CREATE INDEX IX_CuonSach_Barcode
ON CuonSach(Barcode);

CREATE INDEX IX_MuonTra_TrangThai
ON MuonTra(TrangThai);

CREATE INDEX IX_DocGia_HoTen
ON DocGia(HoTen);
GO


INSERT INTO ChucVu
VALUES
('ADMIN',N'QuanTri');

INSERT INTO NhanVien
(
    MaNV,
    HoTen,
    Email,
    MatKhau,
    MaChucVu
)
VALUES
(
    'NV001',
    N'Administrator',
    'admin@gmail.com',
    '123456',
    'ADMIN'
);

INSERT INTO TheLoai
VALUES
('TL001',N'Công Nghệ',N'Sách CNTT'),

('TL002',N'Kinh Tế',N'Sách kinh tế'),

('TL003',N'Ngoại Ngữ',N'Sách tiếng Anh');

ALTER TABLE DocGia
ADD AnhDaiDien VARCHAR(255);


INSERT INTO NhaXuatBan
VALUES
('NXB001',N'NXB Trẻ',N'HCM','0123456789','nxb@gmail.com');

INSERT INTO KeSach
VALUES
('K001',N'Kệ A1',N'Tầng 1');

INSERT INTO Sach
(
    MaSach,
    ISBN,
    Barcode,
    TenSach,
    MaTheLoai,
    MaNXB,
    NamXuatBan,
    NgonNgu,
    SoTrang,
    MoTa,
    AnhBia,
    SoLuongKho,
    SoLuongKhaDung,
    MaKe,
    TrangThai,
    NgayTao
)
VALUES
(
    'S001',
    'ISBN001',
    'BAR001',
    N'Lập trình ASP.NET Core',
    'TL001',
    'NXB001',
    2025,
    N'Tiếng Việt',
    500,
    N'Sách lập trình',
    '',
    10,
    10,
    'K001',
    1,
    GETDATE()
);
INSERT INTO CuonSach
VALUES
('CS001','S001','BC001',N'Có Sẵn',GETDATE()),
('CS002','S001','BC002',N'Có Sẵn',GETDATE()),
('CS003','S001','BC003',N'Có Sẵn',GETDATE());

ALTER TABLE MuonTra
ADD DaGiaHan BIT DEFAULT 0;

UPDATE MuonTra
SET DaGiaHan = 0
WHERE DaGiaHan IS NULL;

CREATE TABLE Reservation (
    MaReservation INT IDENTITY(1,1) PRIMARY KEY,

    MaDocGia VARCHAR(10),

    MaSach VARCHAR(10),

    NgayDat DATETIME DEFAULT GETDATE(),

    TrangThai NVARCHAR(50)
);
DROP TABLE IF EXISTS Reservation;
GO

CREATE TABLE Reservation
(
    MaReservation INT IDENTITY(1,1) PRIMARY KEY,

    MaDocGia VARCHAR(10),

    MaSach VARCHAR(10),

    NgayDat DATETIME DEFAULT GETDATE(),

    TrangThai NVARCHAR(50),

    CONSTRAINT FK_Reservation_DocGia
        FOREIGN KEY (MaDocGia)
        REFERENCES DocGia(MaDocGia),

    CONSTRAINT FK_Reservation_Sach
        FOREIGN KEY (MaSach)
        REFERENCES Sach(MaSach)
);
GO

UPDATE NhanVien
SET MatKhau =
'$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy'
WHERE Email = 'admin@gmail.com';

INSERT INTO ChucVu
VALUES
('LIB',N'Thủ Thư'),

('STU',N'Sinh Viên');


INSERT INTO NhanVien
(
    MaNV,
    HoTen,
    Email,
    MatKhau,
    MaChucVu,
    TrangThai
)
VALUES
(
    'NV002',
    N'Thủ Thư',
    'lib@gmail.com',
    '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy',
    'LIB',
    1
);

UPDATE NhanVien
SET MatKhau = '$2a$11$TWRxtYMzItcW71Ttz0BEhenFgK98IQweUzgJsYJBcEJ3OriCWY0fK'
WHERE Email = 'admin@gmail.com'

-- mk thuthu123
UPDATE NhanVien
SET MatKhau = '$2a$11$qn4WudDMhaIbjKyCh0ZauOg8wyBbxZDVeybouP3i6RdEh1epjyz2C'
WHERE Email = 'lib@gmail.com'

IF COL_LENGTH('Wishlist', 'Id') IS NULL
BEGIN
    ALTER TABLE Wishlist
    ADD Id INT IDENTITY(1,1)
END

IF COL_LENGTH('ChiTietMuonTra', 'Id') IS NULL
BEGIN
    ALTER TABLE ChiTietMuonTra
    ADD Id INT IDENTITY(1,1)
END

SELECT * FROM CuonSach

SELECT * FROM ChiTietMuonTra

ALTER TABLE ChiTietMuonTra
ADD MaSach VARCHAR(10)

ALTER TABLE ChiTietMuonTra
ADD CONSTRAINT FK_ChiTietMuonTra_Sach
FOREIGN KEY (MaSach)
REFERENCES Sach(MaSach)

ALTER TABLE ChiTietMuonTra
ADD SoLuong INT NOT NULL DEFAULT 1

ALTER TABLE ChiTietMuonTra
ADD TienPhat DECIMAL(18,2) NOT NULL DEFAULT 0