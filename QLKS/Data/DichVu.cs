using System;
using System.Collections.Generic;

namespace QLKS.Data;

public partial class DichVu
{
    public int MaDichVu { get; set; }

    public string TenDichVu { get; set; } = null!;

    public decimal DonGia { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<SuDungDichVu> SuDungDichVus { get; set; } = new List<SuDungDichVu>();
}
