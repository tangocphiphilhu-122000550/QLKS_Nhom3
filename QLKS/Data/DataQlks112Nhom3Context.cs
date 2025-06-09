using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QLKS.Data;

public partial class DataQlks112Nhom3Context : DbContext
{
    public DataQlks112Nhom3Context()
    {
    }

    public DataQlks112Nhom3Context(DbContextOptions<DataQlks112Nhom3Context> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<DatPhong> DatPhongs { get; set; }

    public virtual DbSet<DichVu> DichVus { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<Phong> Phongs { get; set; }

    public virtual DbSet<PhuThu> PhuThus { get; set; }

    public virtual DbSet<SuDungDichVu> SuDungDichVus { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=118.69.126.49;Database=data_QLKS_112_Nhom3;User ID=user_112_nhom3;Password=123456789;Encrypt=False;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => e.MaChiTietHoaDon).HasName("PK__ChiTietH__CFF2C426A32FF09F");

            entity.ToTable("ChiTietHoaDon", tb => tb.HasTrigger("trg_ChiTietHoaDon_InsertUpdate"));

            entity.Property(e => e.TongTienDichVu).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TongTienPhong).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK__ChiTietHo__MaDat__571DF1D5");

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MaHoaDon)
                .HasConstraintName("FK__ChiTietHo__MaHoa__5629CD9C");
        });

        modelBuilder.Entity<DatPhong>(entity =>
        {
            entity.HasKey(e => e.MaDatPhong).HasName("PK__DatPhong__6344ADEA76DF76C2");

            entity.ToTable("DatPhong", tb =>
                {
                    tb.HasTrigger("trg_DatPhong_Insert");
                    tb.HasTrigger("trg_DatPhong_Update");
                    tb.HasTrigger("trg_DatPhong_Update_UpdateChiTietHoaDon");
                });

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayDat).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NgayNhanPhong).HasColumnType("datetime");
            entity.Property(e => e.NgayTraPhong).HasColumnType("datetime");
            entity.Property(e => e.PhuThu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TongTienPhong).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TrangThai).HasMaxLength(20);

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.DatPhongs)
                .HasForeignKey(d => d.MaKh)
                .HasConstraintName("FK__DatPhong__MaKH__45F365D3");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.DatPhongs)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK__DatPhong__MaNV__44FF419A");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.DatPhongs)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DatPhong__MaPhon__46E78A0C");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaDichVu).HasName("PK__DichVu__C0E6DE8FD2C3841A");

            entity.ToTable("DichVu");

            entity.Property(e => e.DonGia).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MoTa).HasMaxLength(200);
            entity.Property(e => e.TenDichVu).HasMaxLength(100);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHoaDon).HasName("PK__HoaDon__835ED13BF1DC4064");

            entity.ToTable("HoaDon", tb =>
                {
                    tb.HasTrigger("trg_HoaDon_AfterThanhToan_IsActive");
                    tb.HasTrigger("trg_HoaDon_UpdateTongTien");
                });

            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgayLap).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PhuongThucThanhToan).HasMaxLength(50);
            entity.Property(e => e.TongTien).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaKh)
                .HasConstraintName("FK__HoaDon__MaKH__52593CB8");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK__HoaDon__MaNV__534D60F1");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKh).HasName("PK__KhachHan__2725CF1EC4D0264B");

            entity.ToTable("KhachHang");

            entity.HasIndex(e => e.CccdPassport, "UQ__KhachHan__F045CC19A370186A").IsUnique();

            entity.Property(e => e.MaKh).HasColumnName("MaKH");
            entity.Property(e => e.CccdPassport)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CCCD_Passport");
            entity.Property(e => e.GhiChu).HasMaxLength(200);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.QuocTich).HasMaxLength(50);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK__KhachHang__MaDat__797309D9");
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLoaiPhong).HasName("PK__LoaiPhon__230212170E284B3B");

            entity.ToTable("LoaiPhong");

            entity.Property(e => e.GiaCoBan).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.GiaPhongNgay)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TenLoaiPhong).HasMaxLength(50);
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70ACA101B05");

            entity.ToTable("NhanVien");

            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.DiaChi).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.GioiTinh).HasMaxLength(10);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MatKhau).HasMaxLength(64);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.MaVaiTro)
                .HasConstraintName("FK__NhanVien__MaVaiT__32E0915F");
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.HasKey(e => e.MaPhong).HasName("PK__Phong__20BD5E5B3CFE65C1");

            entity.ToTable("Phong");

            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenPhong).HasMaxLength(50);
            entity.Property(e => e.TrangThai).HasMaxLength(20);

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .HasConstraintName("FK__Phong__MaLoaiPho__3F466844");
        });

        modelBuilder.Entity<PhuThu>(entity =>
        {
            entity.HasKey(e => e.MaPhuThu).HasName("PK__PhuThu__FD0E3BFA7AFD1EBE");

            entity.ToTable("PhuThu");

            entity.Property(e => e.GiaPhuThuTheoGio)
                .HasDefaultValue(50000m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GiaPhuThuTheoNgay)
                .HasDefaultValue(50000m)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.PhuThus)
                .HasForeignKey(d => d.MaLoaiPhong)
                .HasConstraintName("FK__PhuThu__MaLoaiPh__3B75D760");
        });

        modelBuilder.Entity<SuDungDichVu>(entity =>
        {
            entity.HasKey(e => e.MaSuDung).HasName("PK__SuDungDi__73EF96E91E3DE3EB");

            entity.ToTable("SuDungDichVu", tb =>
                {
                    tb.HasTrigger("trg_SuDungDichVu_Insert");
                    tb.HasTrigger("trg_SuDungDichVu_Insert_UpdateChiTietHoaDon");
                    tb.HasTrigger("trg_SuDungDichVu_Update");
                    tb.HasTrigger("trg_SuDungDichVu_UpdateTongTienPhong");
                });

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NgaySuDung).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ThanhTien).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.SuDungDichVus)
                .HasForeignKey(d => d.MaDatPhong)
                .HasConstraintName("FK__SuDungDic__MaDat__4CA06362");

            entity.HasOne(d => d.MaDichVuNavigation).WithMany(p => p.SuDungDichVus)
                .HasForeignKey(d => d.MaDichVu)
                .HasConstraintName("FK__SuDungDic__MaDic__4D94879B");
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tokens__3214EC07F00020A5");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RefreshTokenExpiry).HasColumnType("datetime");
            entity.Property(e => e.Token1).HasColumnName("Token");
            entity.Property(e => e.TokenExpiry).HasColumnType("datetime");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.Tokens)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tokens_NhanVien");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CF61D2F8D9");

            entity.ToTable("VaiTro");

            entity.Property(e => e.TenVaiTro).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
