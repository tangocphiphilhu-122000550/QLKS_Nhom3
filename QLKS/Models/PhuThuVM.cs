namespace QLKS.Models
{
    public class PhuThuVM
    {
        public int MaPhuThu { get; set; }
        public int? MaLoaiPhong { get; set; }
        public decimal? GiaPhuThuTheoNgay { get; set; }
        public decimal? GiaPhuThuTheoGio { get; set; }
        public string? TenLoaiPhong { get; set; } // Additional field for display
    }

    public class PagedPhuThuResponse
    {
        public List<PhuThuVM> PhuThus { get; set; } = new List<PhuThuVM>();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
} 