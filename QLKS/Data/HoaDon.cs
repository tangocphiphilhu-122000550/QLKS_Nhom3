using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class HoaDon
{
    public int MaHoaDon { get; set; }

    public int? MaKh { get; set; }

    public int? MaNv { get; set; }

    public DateOnly? NgayLap { get; set; }

    public decimal? TongTien { get; set; }

    public string? PhuongThucThanhToan { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual KhachHang? MaKhNavigation { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }
}
