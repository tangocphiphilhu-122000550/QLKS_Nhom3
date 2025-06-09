using QLKS.Data;

namespace QLKS.Models
{
    public class PhuThuVM
    {
        public int? MaLoaiPhong { get; set; }

        public decimal? GiaPhuThuTheoNgay { get; set; }

        public decimal? GiaPhuThuTheoGio { get; set; }
        public int MaPhuThu { get; internal set; }
    }
    public  class PhuThuGetall
    {
        public int MaPhuThu { get; set; }

        public int? MaLoaiPhong { get; set; }

        public decimal? GiaPhuThuTheoNgay { get; set; }

        public decimal? GiaPhuThuTheoGio { get; set; }
    }



    public class PagedPhuThuResponse
    {
        public List<PhuThuGetall> PhuThus { get; set; } = new List<PhuThuGetall>();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
} 
