using Microsoft.EntityFrameworkCore;
using SmartLib.Web.Models;

namespace SmartLib.Web.Data;

public class SmartLibDbContext : DbContext
{
    public SmartLibDbContext(DbContextOptions<SmartLibDbContext> options)
        : base(options) { }

    public DbSet<Sach> Saches { get; set; }
    public DbSet<ChucVu> ChucVus { get; set; }
    public DbSet<TheLoai> TheLoais { get; set; }
    public DbSet<NhaXuatBan> NhaXuatBans { get; set; }
    public DbSet<KeSach> KeSaches { get; set; }
    public DbSet<CuonSach> CuonSaches { get; set; }
    public DbSet<TacGia> TacGias { get; set; }
    public DbSet<Sach_TacGia> SachTacGias { get; set; }
    public DbSet<DocGia> DocGias { get; set; }
    public DbSet<NhanVien> NhanViens { get; set; }
    public DbSet<MuonTra> MuonTras { get; set; }
    public DbSet<ChiTietMuonTra> ChiTietMuonTras { get; set; }
    public DbSet<DanhGiaSach> DanhGiaSaches { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<WishlistFolder> WishlistFolders { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ChiTietDatTruoc> ChiTietDatTruocs { get; set; }
    public DbSet<WishlistPreference> WishlistPreferences { get; set; }
    public DbSet<NhatKyHoatDong> NhatKyHoatDongs { get; set; }
    public DbSet<ThongBao> ThongBaos { get; set; }
    public DbSet<Ebook> Ebooks { get; set; }
    public DbSet<GoogleOtpTemp> GoogleOtpTemps { get; set; }
    public DbSet<TheThuVien> TheThiViens { get; set; }
    public DbSet<NhomChucNang> NhomChucNangs { get; set; }
    public DbSet<ChucNang> ChucNangs { get; set; }
    public DbSet<PhanQuyenNhanVien> PhanQuyenNhanViens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Sach_TacGia: composite primary key ──────────────────
        modelBuilder.Entity<Sach_TacGia>()
            .HasKey(x => new { x.MaSach, x.MaTacGia });

        // ── MuonTra → DocGia (restrict delete) ──────────────────
        modelBuilder.Entity<MuonTra>()
            .HasOne(x => x.DocGia)
            .WithMany(x => x.MuonTras)
            .HasForeignKey(x => x.MaDocGia)
            .OnDelete(DeleteBehavior.Restrict);

        // ── MuonTra → NhanVien (restrict delete) ────────────────
        modelBuilder.Entity<MuonTra>()
            .HasOne(x => x.NhanVien)
            .WithMany(x => x.MuonTras)
            .HasForeignKey(x => x.MaNV)
            .OnDelete(DeleteBehavior.Restrict);

        // ── ChiTietMuonTra → MuonTra (cascade delete) ───────────
        modelBuilder.Entity<ChiTietMuonTra>()
            .HasOne(x => x.MuonTra)
            .WithMany(x => x.ChiTietMuonTras)
            .HasForeignKey(x => x.MaPhieu)
            .OnDelete(DeleteBehavior.Cascade);

        // ── WishlistFolder → Wishlist ────────────────────────────
        modelBuilder.Entity<Wishlist>()
            .HasOne(x => x.Folder)
            .WithMany(x => x.Wishlists)
            .HasForeignKey(x => x.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── TheThuVien → DocGia: khi xóa DocGia → gỡ liên kết thẻ (SetNull) ──
        modelBuilder.Entity<TheThuVien>()
            .HasOne(x => x.DocGia)
            .WithMany(x => x.TheThiViens)
            .HasForeignKey(x => x.MaDocGia)
            .OnDelete(DeleteBehavior.SetNull);

        // ── NhatKyHoatDong → NhanVien: khi xóa NV → giữ log (SetNull) ─────
        modelBuilder.Entity<NhatKyHoatDong>()
            .HasOne(x => x.NhanVien)
            .WithMany(x => x.NhatKyHoatDongs)
            .HasForeignKey(x => x.MaNV)
            .OnDelete(DeleteBehavior.SetNull);

        // ── KeSach → NXB / TheLoai (SetNull khi xóa) ────────────────────────
        modelBuilder.Entity<KeSach>()
            .HasOne(x => x.NXBPhuTrach)
            .WithMany()
            .HasForeignKey(x => x.MaNXBPhuTrach)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KeSach>()
            .HasOne(x => x.TheLoaiPhuTrach)
            .WithMany()
            .HasForeignKey(x => x.MaTheLoaiPhuTrach)
            .OnDelete(DeleteBehavior.SetNull);

        // ── ThongBao → Sach (SetNull khi xóa) ───────────────────────────────
        modelBuilder.Entity<ThongBao>()
            .HasOne(x => x.Sach)
            .WithMany()
            .HasForeignKey(x => x.MaSach)
            .OnDelete(DeleteBehavior.SetNull);

        // ── ThongBao → DocGia: khi xóa độc giả → giữ thông báo, gỡ liên kết (SetNull) ──
        modelBuilder.Entity<ThongBao>()
            .HasOne(x => x.DocGia)
            .WithMany()
            .HasForeignKey(x => x.MaDocGia)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Ebook → Sach (restrict: không cho xóa sách khi còn ebook đính kèm) ──
        modelBuilder.Entity<Ebook>()
            .HasOne(x => x.Sach)
            .WithMany()
            .HasForeignKey(x => x.MaSach)
            .OnDelete(DeleteBehavior.Restrict);

        // ── PHÂN QUYỀN CHI TIẾT ──────────────────────────────────────────────
        // ChucNang → NhomChucNang (restrict: không cho xóa nhóm khi còn chức năng con)
        modelBuilder.Entity<ChucNang>()
            .HasOne(x => x.NhomChucNang)
            .WithMany(x => x.ChucNangs)
            .HasForeignKey(x => x.MaNhom)
            .OnDelete(DeleteBehavior.Restrict);

        // PhanQuyenNhanVien → NhanVien (cascade: xóa NV thì xóa luôn quyền của NV đó)
        modelBuilder.Entity<PhanQuyenNhanVien>()
            .HasOne(x => x.NhanVien)
            .WithMany(x => x.PhanQuyens)
            .HasForeignKey(x => x.MaNV)
            .OnDelete(DeleteBehavior.Cascade);

        // PhanQuyenNhanVien → ChucNang (cascade: xóa chức năng thì xóa luôn dòng quyền liên quan)
        modelBuilder.Entity<PhanQuyenNhanVien>()
            .HasOne(x => x.ChucNang)
            .WithMany(x => x.PhanQuyens)
            .HasForeignKey(x => x.MaChucNang)
            .OnDelete(DeleteBehavior.Cascade);

        // Mỗi nhân viên chỉ có 1 dòng quyền cho mỗi chức năng
        modelBuilder.Entity<PhanQuyenNhanVien>()
            .HasIndex(x => new { x.MaNV, x.MaChucNang })
            .IsUnique();
    }
}