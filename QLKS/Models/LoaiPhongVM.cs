namespace QLKS.Models
{
    // ViewModel cơ bản cho loại phòng
    public class LoaiPhongVM
    {
        public string TenLoaiPhong { get; set; } = null!;
        public decimal GiaCoBan { get; set; }
        public int SoNguoiToiDa { get; set; }
    }

    // Model đầy đủ với khóa chính
    public class LoaiPhongMD : LoaiPhongVM
    {
        public int MaLoaiPhong { get; set; }
    }
    public class PagedLoaiPhongResponse
    {
        public List<LoaiPhongMD> LoaiPhongs { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}