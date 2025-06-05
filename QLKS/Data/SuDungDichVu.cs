using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class SuDungDichVu
{
    public int MaSuDung { get; set; }

    public int? MaDatPhong { get; set; }

    public int? MaDichVu { get; set; }

    public int SoLuong { get; set; }

    public DateOnly? NgaySuDung { get; set; }

    public DateOnly? NgayKetThuc { get; set; }

    public decimal? ThanhTien { get; set; }

    public bool IsActive { get; set; }

    public virtual DatPhong? MaDatPhongNavigation { get; set; }

    public virtual DichVu? MaDichVuNavigation { get; set; }
}
