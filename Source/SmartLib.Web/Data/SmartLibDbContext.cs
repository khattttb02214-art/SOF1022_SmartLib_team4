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
    public DbSet<WishlistPreference> WishlistPreferences { get; set; }
    public DbSet<NhatKyHoatDong> NhatKyHoatDongs { get; set; }
    public DbSet<ThongBao> ThongBaos { get; set; }
    public DbSet<TheThuVien> TheThiViens { get; set; }

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
    }
}
