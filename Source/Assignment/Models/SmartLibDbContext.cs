using System;
using System.Collections.Generic;
using Assignment.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Models;

public partial class SmartLibDbContext : DbContext
{
    public SmartLibDbContext()
    {
    }

    public SmartLibDbContext(DbContextOptions<SmartLibDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BackupDuLieu> BackupDuLieus { get; set; }

    public virtual DbSet<ChiTietMuonTra> ChiTietMuonTras { get; set; }

    public virtual DbSet<ChucVu> ChucVus { get; set; }

    public virtual DbSet<CuonSach> CuonSaches { get; set; }

    public virtual DbSet<DanhGiaSach> DanhGiaSaches { get; set; }

    public virtual DbSet<DocGium> DocGia { get; set; }

    public virtual DbSet<Ebook> Ebooks { get; set; }

    public virtual DbSet<KeSach> KeSaches { get; set; }

    public virtual DbSet<MuonTra> MuonTras { get; set; }

    public virtual DbSet<NhaXuatBan> NhaXuatBans { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<NhatKyHoatDong> NhatKyHoatDongs { get; set; }

    public virtual DbSet<Sach> Saches { get; set; }

    public virtual DbSet<TacGium> TacGia { get; set; }

    public virtual DbSet<TheLoai> TheLoais { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=SmartLibDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupDuLieu>(entity =>
        {
            entity.HasKey(e => e.MaBackup).HasName("PK__BackupDu__B633C829B5E833B7");

            entity.ToTable("BackupDuLieu");

            entity.Property(e => e.MaNv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNV");
            entity.Property(e => e.NgayBackup)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenFile)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.BackupDuLieus)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK_Backup_NhanVien");
        });

        modelBuilder.Entity<ChiTietMuonTra>(entity =>
        {
            entity.HasKey(e => new { e.MaPhieu, e.MaCuonSach }).HasName("PK__ChiTietM__EC6059665AED2B5D");

            entity.ToTable("ChiTietMuonTra");

            entity.Property(e => e.MaPhieu)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaCuonSach)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.GhiChu).HasMaxLength(255);

            entity.HasOne(d => d.MaCuonSachNavigation).WithMany(p => p.ChiTietMuonTras)
                .HasForeignKey(d => d.MaCuonSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTMT_CuonSach");

            entity.HasOne(d => d.MaPhieuNavigation).WithMany(p => p.ChiTietMuonTras)
                .HasForeignKey(d => d.MaPhieu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTMT_MuonTra");
        });

        modelBuilder.Entity<ChucVu>(entity =>
        {
            entity.HasKey(e => e.MaChucVu).HasName("PK__ChucVu__D4639533F744FC27");

            entity.ToTable("ChucVu");

            entity.HasIndex(e => e.TenChucVu, "UQ__ChucVu__A7E2123EBEC8BDDD").IsUnique();

            entity.Property(e => e.MaChucVu)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenChucVu).HasMaxLength(50);
        });

        modelBuilder.Entity<CuonSach>(entity =>
        {
            entity.HasKey(e => e.MaCuonSach).HasName("PK__CuonSach__A00E686DEB2C1E6D");

            entity.ToTable("CuonSach");

            entity.HasIndex(e => e.Barcode, "IX_CuonSach_Barcode");

            entity.HasIndex(e => e.Barcode, "UQ__CuonSach__177800D3E244FA1D").IsUnique();

            entity.Property(e => e.MaCuonSach)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Barcode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayNhap).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Có Sẵn");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.CuonSaches)
                .HasForeignKey(d => d.MaSach)
                .HasConstraintName("FK_CuonSach_Sach");
        });

        modelBuilder.Entity<DanhGiaSach>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGiaS__AA9515BF36DC31D6");

            entity.ToTable("DanhGiaSach");

            entity.Property(e => e.MaDocGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDung).HasMaxLength(1000);

            entity.HasOne(d => d.MaDocGiaNavigation).WithMany(p => p.DanhGiaSaches)
                .HasForeignKey(d => d.MaDocGia)
                .HasConstraintName("FK_DanhGia_DocGia");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.DanhGiaSaches)
                .HasForeignKey(d => d.MaSach)
                .HasConstraintName("FK_DanhGia_Sach");
        });

        modelBuilder.Entity<DocGium>(entity =>
        {
            entity.HasKey(e => e.MaDocGia).HasName("PK__DocGia__F165F945181AC0FF");

            entity.HasIndex(e => e.HoTen, "IX_DocGia_HoTen");

            entity.HasIndex(e => e.Email, "UQ__DocGia__A9D105340618B667").IsUnique();

            entity.Property(e => e.MaDocGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.Khoa).HasMaxLength(100);
            entity.Property(e => e.Lop).HasMaxLength(50);
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NgayTaoThe).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TrangThaiThe).HasDefaultValue(true);
        });

        modelBuilder.Entity<Ebook>(entity =>
        {
            entity.HasKey(e => e.MaEbook).HasName("PK__Ebook__BB84FEC668CFAFCC");

            entity.ToTable("Ebook");

            entity.Property(e => e.DinhDangFile)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DuongDanFile)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayTaiLen)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenFile).HasMaxLength(255);

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.Ebooks)
                .HasForeignKey(d => d.MaSach)
                .HasConstraintName("FK_Ebook_Sach");
        });

        modelBuilder.Entity<KeSach>(entity =>
        {
            entity.HasKey(e => e.MaKe).HasName("PK__KeSach__2725CF7DEA2DAAE4");

            entity.ToTable("KeSach");

            entity.Property(e => e.MaKe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenKe).HasMaxLength(50);
            entity.Property(e => e.ViTri).HasMaxLength(100);
        });

        modelBuilder.Entity<MuonTra>(entity =>
        {
            entity.HasKey(e => e.MaPhieu).HasName("PK__MuonTra__2660BFE0354C6DEC");

            entity.ToTable("MuonTra");

            entity.HasIndex(e => e.TrangThai, "IX_MuonTra_TrangThai");

            entity.Property(e => e.MaPhieu)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.MaDocGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaNv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNV");
            entity.Property(e => e.NgayMuon).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TienPhat)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa Trả");

            entity.HasOne(d => d.MaDocGiaNavigation).WithMany(p => p.MuonTras)
                .HasForeignKey(d => d.MaDocGia)
                .HasConstraintName("FK_MuonTra_DocGia");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.MuonTras)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK_MuonTra_NhanVien");
        });

        modelBuilder.Entity<NhaXuatBan>(entity =>
        {
            entity.HasKey(e => e.MaNxb).HasName("PK__NhaXuatB__3A19482CC6475F87");

            entity.ToTable("NhaXuatBan");

            entity.Property(e => e.MaNxb)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNXB");
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TenNxb)
                .HasMaxLength(100)
                .HasColumnName("TenNXB");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70AE28E63A5");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.Email, "UQ__NhanVien__A9D10534BE4C3901").IsUnique();

            entity.Property(e => e.MaNv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNV");
            entity.Property(e => e.AnhDaiDien)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MaChucVu)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaChucVuNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.MaChucVu)
                .HasConstraintName("FK_NhanVien_ChucVu");
        });

        modelBuilder.Entity<NhatKyHoatDong>(entity =>
        {
            entity.HasKey(e => e.MaLog).HasName("PK__NhatKyHo__3B98D24AEEA802AE");

            entity.ToTable("NhatKyHoatDong");

            entity.Property(e => e.HanhDong).HasMaxLength(255);
            entity.Property(e => e.MaNv)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNV");
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.NhatKyHoatDongs)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK_NhatKy_NhanVien");
        });

        modelBuilder.Entity<Sach>(entity =>
        {
            entity.HasKey(e => e.MaSach).HasName("PK__Sach__B235742D6AE0FA48");

            entity.ToTable("Sach");

            entity.HasIndex(e => e.Isbn, "IX_Sach_ISBN");

            entity.HasIndex(e => e.TenSach, "IX_Sach_TenSach");

            entity.HasIndex(e => e.Barcode, "UQ__Sach__177800D3F9A0609B").IsUnique();

            entity.HasIndex(e => e.Isbn, "UQ__Sach__447D36EA19B4EC2F").IsUnique();

            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.AnhBia)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Barcode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Isbn)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ISBN");
            entity.Property(e => e.MaKe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaNxb)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNXB");
            entity.Property(e => e.MaTheLoai)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NgonNgu).HasMaxLength(50);
            entity.Property(e => e.SoLuongKhaDung).HasDefaultValue(0);
            entity.Property(e => e.SoLuongKho).HasDefaultValue(0);
            entity.Property(e => e.TenSach).HasMaxLength(200);
            entity.Property(e => e.TrangThai).HasDefaultValue(true);

            entity.HasOne(d => d.MaKeNavigation).WithMany(p => p.Saches)
                .HasForeignKey(d => d.MaKe)
                .HasConstraintName("FK_Sach_KeSach");

            entity.HasOne(d => d.MaNxbNavigation).WithMany(p => p.Saches)
                .HasForeignKey(d => d.MaNxb)
                .HasConstraintName("FK_Sach_NXB");

            entity.HasOne(d => d.MaTheLoaiNavigation).WithMany(p => p.Saches)
                .HasForeignKey(d => d.MaTheLoai)
                .HasConstraintName("FK_Sach_TheLoai");

            entity.HasMany(d => d.MaTacGia).WithMany(p => p.MaSaches)
                .UsingEntity<Dictionary<string, object>>(
                    "SachTacGium",
                    r => r.HasOne<TacGium>().WithMany()
                        .HasForeignKey("MaTacGia")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_SachTacGia_TacGia"),
                    l => l.HasOne<Sach>().WithMany()
                        .HasForeignKey("MaSach")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_SachTacGia_Sach"),
                    j =>
                    {
                        j.HasKey("MaSach", "MaTacGia").HasName("PK__Sach_Tac__CD1192589ABF0743");
                        j.ToTable("Sach_TacGia");
                        j.IndexerProperty<string>("MaSach")
                            .HasMaxLength(10)
                            .IsUnicode(false);
                        j.IndexerProperty<string>("MaTacGia")
                            .HasMaxLength(10)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<TacGium>(entity =>
        {
            entity.HasKey(e => e.MaTacGia).HasName("PK__TacGia__F24E6756E305A5E1");

            entity.Property(e => e.MaTacGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.QuocTich).HasMaxLength(100);
            entity.Property(e => e.TenTacGia).HasMaxLength(100);
        });

        modelBuilder.Entity<TheLoai>(entity =>
        {
            entity.HasKey(e => e.MaTheLoai).HasName("PK__TheLoai__D73FF34A11A53AD2");

            entity.ToTable("TheLoai");

            entity.HasIndex(e => e.TenTheLoai, "UQ__TheLoai__327F958FB85C312D").IsUnique();

            entity.Property(e => e.MaTheLoai)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MoTa).HasMaxLength(500);
            entity.Property(e => e.TenTheLoai).HasMaxLength(100);
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54E5FF2231F");

            entity.ToTable("ThongBao");

            entity.Property(e => e.DaDoc).HasDefaultValue(false);
            entity.Property(e => e.MaDocGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDung).HasMaxLength(500);

            entity.HasOne(d => d.MaDocGiaNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaDocGia)
                .HasConstraintName("FK_ThongBao_DocGia");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => new { e.MaDocGia, e.MaSach }).HasName("PK__Wishlist__3A46AE07EF953C30");

            entity.ToTable("Wishlist");

            entity.Property(e => e.MaDocGia)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaSach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayThem).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MaDocGiaNavigation).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.MaDocGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wishlist_DocGia");

            entity.HasOne(d => d.MaSachNavigation).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.MaSach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wishlist_Sach");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
