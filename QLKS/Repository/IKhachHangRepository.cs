using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;

namespace QLKS.Repository
{
    public interface IKhachHangRepository
    {
        Task<PagedKhachHangResponse> GetAllKhachHang(int pageNumber, int pageSize);
        Task<List<KhachHangMD>> GetKhachHangByName(string hoTen);
        Task<KhachHangVM> AddKhachHang(KhachHangVM khachHang);
        Task<bool> UpdateKhachHang(string hoTen, KhachHangVM khachHangVM);
        Task<bool> DeleteKhachHang(string hoTen);
    }

    public class KhachHangRepository : IKhachHangRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public KhachHangRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedKhachHangResponse> GetAllKhachHang(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.KhachHangs
                .AsNoTracking()
                .Where(kh => kh.IsActive == true);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var khachHangs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(kh => new KhachHangMD
                {
                    MaKh = kh.MaKh,
                    HoTen = kh.HoTen,
                    CccdPassport = kh.CccdPassport,
                    SoDienThoai = kh.SoDienThoai,
                    QuocTich = kh.QuocTich,
                    GhiChu = kh.GhiChu,
                    MaDatPhong = kh.MaDatPhong
                })
                .ToListAsync();

            return new PagedKhachHangResponse
            {
                KhachHangs = khachHangs,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<KhachHangMD>> GetKhachHangByName(string hoTen)
        {
            return await _context.KhachHangs
                .AsNoTracking()
                .Where(kh => kh.HoTen.Contains(hoTen) && kh.IsActive == true)
                .Select(kh => new KhachHangMD
                {
                    MaKh = kh.MaKh,
                    HoTen = kh.HoTen,
                    CccdPassport = kh.CccdPassport,
                    SoDienThoai = kh.SoDienThoai,
                    QuocTich = kh.QuocTich,
                    GhiChu = kh.GhiChu,
                    MaDatPhong = kh.MaDatPhong
                })
                .ToListAsync();
        }

        public async Task<KhachHangVM> AddKhachHang(KhachHangVM khachHangVM)
        {
            if (string.IsNullOrEmpty(khachHangVM.HoTen))
            {
                throw new ArgumentException("Họ tên khách hàng không hợp lệ.");
            }

            var khachHang = new KhachHang
            {
                HoTen = khachHangVM.HoTen,
                CccdPassport = khachHangVM.CccdPassport,
                SoDienThoai = khachHangVM.SoDienThoai,
                QuocTich = khachHangVM.QuocTich,
                GhiChu = khachHangVM.GhiChu,
                MaDatPhong = null, // Không yêu cầu MaDatPhong khi tạo khách hàng
                IsActive = true
            };

            _context.KhachHangs.Add(khachHang);
            await _context.SaveChangesAsync();

            return new KhachHangVM
            {
                HoTen = khachHang.HoTen,
                CccdPassport = khachHang.CccdPassport,
                SoDienThoai = khachHang.SoDienThoai,
                QuocTich = khachHang.QuocTich,
                GhiChu = khachHang.GhiChu,
                MaDatPhong = khachHang.MaDatPhong // Trả về null nếu chưa liên kết
            };
        }

        public async Task<bool> UpdateKhachHang(string hoTen, KhachHangVM khachHangVM)
        {
            if (string.IsNullOrEmpty(khachHangVM.HoTen))
            {
                throw new ArgumentException("Họ tên khách hàng không hợp lệ.");
            }

            var existingKhachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.HoTen == hoTen && kh.IsActive == true);
            if (existingKhachHang == null)
            {
                return false;
            }

            existingKhachHang.MaDatPhong = khachHangVM.MaDatPhong; // Cập nhật MaDatPhong nếu có
            existingKhachHang.HoTen = khachHangVM.HoTen;
            existingKhachHang.CccdPassport = khachHangVM.CccdPassport;
            existingKhachHang.SoDienThoai = khachHangVM.SoDienThoai;
            existingKhachHang.QuocTich = khachHangVM.QuocTich;
            existingKhachHang.GhiChu = khachHangVM.GhiChu;

            _context.KhachHangs.Update(existingKhachHang);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteKhachHang(string hoTen)
        {
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.HoTen == hoTen && kh.IsActive == true);
            if (khachHang == null)
            {
                return false;
            }

            khachHang.IsActive = false;
            _context.KhachHangs.Update(khachHang);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}