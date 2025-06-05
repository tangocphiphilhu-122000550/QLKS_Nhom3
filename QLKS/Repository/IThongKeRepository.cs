using Microsoft.EntityFrameworkCore;
using QLKS.Data;

namespace QLKS.Repository
{
    public interface IThongKeRepository
    {
        Task<ThongKeResponse> ThongKeTheoNgay(DateTime ngay);
        Task<ThongKeResponse> ThongKeTheoThang(int nam, int thang);
        Task<ThongKeResponse> ThongKeTheoNam(int nam);
        Task<ThongKeResponse> ThongKeTheoKhoangThoiGian(DateTime tuNgay, DateTime denNgay); 
    }
    public class ThongKeRepository : IThongKeRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public ThongKeRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<ThongKeResponse> ThongKeTheoNgay(DateTime ngay)
        {
            var query = _context.HoaDons
                .Include(hd => hd.MaKhNavigation) // Fixed property name
                .Where(hd => hd.NgayLap.HasValue && hd.NgayLap.Value == DateOnly.FromDateTime(ngay));

            var soLuongKhachHang = await query
                .Select(hd => hd.MaKh)
                .Distinct()
                .CountAsync();

            var tongDoanhThu = await query
                .SumAsync(hd => hd.TongTien ?? 0);

            return new ThongKeResponse
            {
                SoLuongKhachHang = soLuongKhachHang,
                TongDoanhThu = tongDoanhThu,
                ThoiGian = ngay.ToString("dd/MM/yyyy")
            };
        }

        public async Task<ThongKeResponse> ThongKeTheoThang(int nam, int thang)
        {
            var query = _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Where(hd => hd.NgayLap.HasValue &&
                             hd.NgayLap.Value.Year == nam &&
                             hd.NgayLap.Value.Month == thang);

            var soLuongKhachHang = await query
                .Select(hd => hd.MaKh) // Fixed property name
                .Distinct()
                .CountAsync();

            var tongDoanhThu = await query
                .SumAsync(hd => hd.TongTien ?? 0);

            return new ThongKeResponse
            {
                SoLuongKhachHang = soLuongKhachHang,
                TongDoanhThu = tongDoanhThu,
                ThoiGian = $"{thang:00}/{nam}"
            };
        }

        public async Task<ThongKeResponse> ThongKeTheoNam(int nam)
        {
            var query = _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Where(hd => hd.NgayLap.HasValue && hd.NgayLap.Value.Year == nam);

            var soLuongKhachHang = await query
                .Select(hd => hd.MaKh) // Fixed property name
                .Distinct()
                .CountAsync();

            var tongDoanhThu = await query
                .SumAsync(hd => hd.TongTien ?? 0);

            return new ThongKeResponse
            {
                SoLuongKhachHang = soLuongKhachHang,
                TongDoanhThu = tongDoanhThu,
                ThoiGian = nam.ToString()
            };
        }

        public async Task<ThongKeResponse> ThongKeTheoKhoangThoiGian(DateTime tuNgay, DateTime denNgay)
        {
            var query = _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Where(hd => hd.NgayLap.HasValue &&
                             hd.NgayLap.Value >= DateOnly.FromDateTime(tuNgay) &&
                             hd.NgayLap.Value <= DateOnly.FromDateTime(denNgay));

            var soLuongKhachHang = await query
                .Select(hd => hd.MaKh)
                .Distinct()
                .CountAsync();

            var tongDoanhThu = await query
                .SumAsync(hd => hd.TongTien ?? 0);

            return new ThongKeResponse
            {
                SoLuongKhachHang = soLuongKhachHang,
                TongDoanhThu = tongDoanhThu,
                ThoiGian = $"{tuNgay:dd/MM/yyyy} - {denNgay:dd/MM/yyyy}"
            };
        }

    }

    public class ThongKeResponse
    {
        public int SoLuongKhachHang { get; set; }
        public decimal TongDoanhThu { get; set; }
        public string ThoiGian { get; set; }
        public virtual KhachHang? MaKhNavigation { get; set; }
    }
}
