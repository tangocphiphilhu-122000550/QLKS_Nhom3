using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class ChiTietHoaDon
{
    public int MaChiTietHoaDon { get; set; }

    public int? MaHoaDon { get; set; }

    public int? MaDatPhong { get; set; }

    public decimal? TongTienPhong { get; set; }

    public decimal? TongTienDichVu { get; set; }

    public virtual DatPhong? MaDatPhongNavigation { get; set; }

    public virtual HoaDon? MaHoaDonNavigation { get; set; }
}
