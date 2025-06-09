using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class DatPhong
{
    public int MaDatPhong { get; set; }

    public int? MaNv { get; set; }

    public int? MaKh { get; set; }

    public string MaPhong { get; set; } = null!;

    public DateOnly? NgayDat { get; set; }

    public DateTime? NgayNhanPhong { get; set; }

    public DateTime? NgayTraPhong { get; set; }

    public int SoNguoiO { get; set; }

    public decimal? PhuThu { get; set; }

    public string? TrangThai { get; set; }

    public decimal? TongTienPhong { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual ICollection<KhachHang> KhachHangs { get; set; } = new List<KhachHang>();

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;

    public virtual ICollection<SuDungDichVu> SuDungDichVus { get; set; } = new List<SuDungDichVu>();
}
