using QLKS.Data;

namespace QLKS.Models
{
    // ViewModel cơ bản
    public class KhachHangVM
    {
        public int? MaDatPhong { get; set; }
        public string HoTen { get; set; } = null!;
        public string? CccdPassport { get; set; }
        public string? SoDienThoai { get; set; }
        public string? QuocTich { get; set; }
        public string? GhiChu { get; set; }

    }

    // Model đầy đủ với khóa chính và navigation property
    public class KhachHangMD : KhachHangVM
    {
        public int MaKh { get; set; }
    }

    // DTO để truyền dữ liệu
    public class KhachHangDTO
    {
        public string HoTen { get; set; } = null!;
        public string? CccdPassport { get; set; }
        public string? SoDienThoai { get; set; }
        public string? QuocTich { get; set; }
        public string? GhiChu { get; set; }
    }

    public class TenKhachHangVM
    {
        public string HoTen { get; set; } = null!;
    }

    public class PagedKhachHangResponse
    {
        public List<KhachHangMD> KhachHangs { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}