using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class LoaiPhong
{
    public int MaLoaiPhong { get; set; }

    public string TenLoaiPhong { get; set; } = null!;

    public int SoNguoiToiDa { get; set; }

    public decimal GiaCoBan { get; set; }

    public decimal? GiaPhongNgay { get; set; }

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();

    public virtual ICollection<PhuThu> PhuThus { get; set; } = new List<PhuThu>();
}
