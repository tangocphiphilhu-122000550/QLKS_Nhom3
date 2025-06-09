using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class Phong
{
    public string MaPhong { get; set; } = null!;

    public int? MaLoaiPhong { get; set; }

    public string? TenPhong { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();

    public virtual LoaiPhong? MaLoaiPhongNavigation { get; set; }
}
