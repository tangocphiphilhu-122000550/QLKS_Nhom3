using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class PhuThu
{
    public int MaPhuThu { get; set; }

    public int? MaLoaiPhong { get; set; }

    public decimal? GiaPhuThuTheoNgay { get; set; }

    public decimal? GiaPhuThuTheoGio { get; set; }

    public virtual LoaiPhong? MaLoaiPhongNavigation { get; set; }
}
