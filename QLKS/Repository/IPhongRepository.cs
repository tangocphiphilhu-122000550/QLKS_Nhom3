using QLKS.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLKS.Data;
using Microsoft.EntityFrameworkCore;

namespace QLKS.Repository
{
    public interface IPhongRepository
    {
        Task<PagedPhongResponse> GetAllAsync(int pageNumber, int pageSize);
        Task<PhongMD> GetByIdAsync(string MaPhong);
        Task<PhongMD> AddPhongAsync(PhongAddVM phongVM);
        Task<bool> EditPhongAsync(string MaPhong, PhongEditVM phongVM);
        Task<bool> DeletePhongAsync(string MaPhong);
        Task<PagedPhongResponse> GetByTrangThaiAsync(string trangThai, int pageNumber, int pageSize);
        Task<bool> UpdateTrangThaiAsync(string maPhong, string trangThai);
        Task<PagedPhongResponse> GetByLoaiPhongAsync(int maLoaiPhong, int pageNumber, int pageSize);
        Task<Dictionary<string, int>> GetRoomStatusStatisticsAsync();
        Task<bool> IsRoomAvailableAsync(string maPhong, DateTime startDate, DateTime endDate);
    }
    public class PhongRepository : IPhongRepository
    {
        private readonly DataQlks112Nhom3Context _context;
        private readonly TimeSpan _cleanupBuffer = TimeSpan.FromHours(2); // Khoảng dọn dẹp: 2 tiếng

        public PhongRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedPhongResponse> GetAllAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Phongs
                .Include(p => p.MaLoaiPhongNavigation);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var phongs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PhongMD
                {
                    MaPhong = p.MaPhong,
                    MaLoaiPhong = p.MaLoaiPhong,
                    TenPhong = p.TenPhong,
                    TrangThai = p.TrangThai,
                    TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong,
                    GiaCoBan = p.MaLoaiPhongNavigation.GiaCoBan,
                    SoNguoiToiDa = p.MaLoaiPhongNavigation.SoNguoiToiDa
                })
                .ToListAsync();

            return new PagedPhongResponse
            {
                Phongs = phongs,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PhongMD> GetByIdAsync(string MaPhong)
        {
            var phong = await _context.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .FirstOrDefaultAsync(u => u.MaPhong == MaPhong);

            if (phong == null)
            {
                return null; // Trả về null để controller xử lý lỗi
            }

            return new PhongMD
            {
                MaPhong = phong.MaPhong,
                MaLoaiPhong = phong.MaLoaiPhong,
                TenPhong = phong.TenPhong,
                TrangThai = phong.TrangThai,
                TenLoaiPhong = phong.MaLoaiPhongNavigation.TenLoaiPhong,
                GiaCoBan = phong.MaLoaiPhongNavigation.GiaCoBan,
                SoNguoiToiDa = phong.MaLoaiPhongNavigation.SoNguoiToiDa
            };
        }

        public async Task<PhongMD> AddPhongAsync(PhongAddVM phongVM)
        {
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(phongVM.MaPhong) ||
                string.IsNullOrWhiteSpace(phongVM.TenPhong))
            {
                throw new ArgumentException("Thông tin phòng không đầy đủ. Vui lòng nhập MaPhong, MaLoaiPhong và TenPhong.");
            }

            // Kiểm tra xem phòng đã tồn tại chưa (dựa trên MaPhong hoặc TenPhong)
            var check = await _context.Phongs
                .FirstOrDefaultAsync(c => c.TenPhong == phongVM.TenPhong || c.MaPhong == phongVM.MaPhong);
            if (check != null)
            {
                throw new ArgumentException("Phòng đã tồn tại");
            }

            // Kiểm tra xem MaLoaiPhong có tồn tại không
            var loaiPhong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == phongVM.MaLoaiPhong);
            if (loaiPhong == null)
            {
                throw new ArgumentException("Loại phòng không tồn tại");
            }

            // Tạo mới phòng với các trường bắt buộc và trạng thái mặc định là "Trống"
            var phong = new Phong
            {
                MaPhong = phongVM.MaPhong,
                MaLoaiPhong = phongVM.MaLoaiPhong,
                TenPhong = phongVM.TenPhong,
                TrangThai = "Trống" // Mặc định trạng thái là "Trống"
            };

            _context.Phongs.Add(phong);
            await _context.SaveChangesAsync();

            return new PhongMD
            {
                MaPhong = phong.MaPhong,
                MaLoaiPhong = phong.MaLoaiPhong,
                TenPhong = phong.TenPhong,
                TrangThai = phong.TrangThai,
                TenLoaiPhong = loaiPhong.TenLoaiPhong,
                GiaCoBan = loaiPhong.GiaCoBan,
                SoNguoiToiDa = loaiPhong.SoNguoiToiDa
            };
        }

        public async Task<bool> EditPhongAsync(string MaPhong, PhongEditVM phongVM)
        {
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(phongVM.TenPhong))
            {
                throw new ArgumentException("Thông tin phòng không đầy đủ. Vui lòng nhập MaLoaiPhong và TenPhong.");
            }

            // Kiểm tra xem phòng có tồn tại không
            var phong = await _context.Phongs
                .SingleOrDefaultAsync(l => l.MaPhong == MaPhong);
            if (phong == null)
            {
                return false; // Trả về false để controller xử lý lỗi
            }

            // Kiểm tra xem MaLoaiPhong có tồn tại không
            var loaiPhong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == phongVM.MaLoaiPhong);
            if (loaiPhong == null)
            {
                throw new ArgumentException("Loại phòng không tồn tại");
            }

            // Kiểm tra xem TenPhong có bị trùng với phòng khác không
            var checkDuplicate = await _context.Phongs
                .FirstOrDefaultAsync(c => c.TenPhong == phongVM.TenPhong && c.MaPhong != MaPhong);
            if (checkDuplicate != null)
            {
                throw new ArgumentException("Tên phòng đã tồn tại");
            }

            // Cập nhật các trường cần thiết
            phong.MaLoaiPhong = phongVM.MaLoaiPhong;
            phong.TenPhong = phongVM.TenPhong;
            // Không cập nhật TrangThai, giữ nguyên giá trị hiện tại

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePhongAsync(string MaPhong)
        {
            // Kiểm tra xem phòng có tồn tại không
            var phong = await _context.Phongs
                .SingleOrDefaultAsync(l => l.MaPhong == MaPhong);
            if (phong == null)
            {
                return false; // Trả về false để controller xử lý lỗi
            }

            // Kiểm tra xem phòng có đang được thuê hoặc đặt không
            var currentDateTime = DateTime.Now;
            var activeBookings = await _context.DatPhongs
                .Where(dp => dp.MaPhong == MaPhong
                    && dp.TrangThai != "Hủy"
                    && dp.TrangThai != "Hoàn thành"
                    && currentDateTime >= dp.NgayNhanPhong
                    && currentDateTime <= dp.NgayTraPhong)
                .AnyAsync();

            if (activeBookings)
            {
                throw new ArgumentException("Phòng đang được thuê hoặc đặt. Vui lòng đợi khách trả phòng hoặc chuyển khách sang phòng khác trước khi xóa.");
            }

            // Xóa phòng
            _context.Phongs.Remove(phong);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PagedPhongResponse> GetByTrangThaiAsync(string trangThai, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .Where(p => p.TrangThai == trangThai);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var phongs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PhongMD
                {
                    MaPhong = p.MaPhong,
                    MaLoaiPhong = p.MaLoaiPhong,
                    TenPhong = p.TenPhong,
                    TrangThai = p.TrangThai,
                    TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong,
                    GiaCoBan = p.MaLoaiPhongNavigation.GiaCoBan,
                    SoNguoiToiDa = p.MaLoaiPhongNavigation.SoNguoiToiDa
                })
                .ToListAsync();

            return new PagedPhongResponse
            {
                Phongs = phongs,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<bool> UpdateTrangThaiAsync(string maPhong, string trangThai)
        {
            var phong = await _context.Phongs
                .SingleOrDefaultAsync(p => p.MaPhong == maPhong);
            if (phong == null)
            {
                return false; // Trả về false để controller xử lý lỗi
            }

            var validTrangThai = new[] { "Trống", "Đang sử dụng", "Bảo trì" };
            if (!validTrangThai.Contains(trangThai, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Trạng thái không hợp lệ. Chỉ cho phép: Trống, Đang sử dụng, Bảo trì");
            }

            phong.TrangThai = trangThai;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedPhongResponse> GetByLoaiPhongAsync(int maLoaiPhong, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .Where(p => p.MaLoaiPhong == maLoaiPhong);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var phongs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PhongMD
                {
                    MaPhong = p.MaPhong,
                    MaLoaiPhong = p.MaLoaiPhong,
                    TenPhong = p.TenPhong,
                    TrangThai = p.TrangThai,
                    TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong,
                    GiaCoBan = p.MaLoaiPhongNavigation.GiaCoBan,
                    SoNguoiToiDa = p.MaLoaiPhongNavigation.SoNguoiToiDa
                })
                .ToListAsync();

            return new PagedPhongResponse
            {
                Phongs = phongs,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<Dictionary<string, int>> GetRoomStatusStatisticsAsync()
        {
            var statistics = await _context.Phongs
                .GroupBy(p => p.TrangThai)
                .Select(g => new { TrangThai = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TrangThai ?? "Không xác định", x => x.Count);

            return statistics;
        }

        public async Task<bool> IsRoomAvailableAsync(string maPhong, DateTime startDate, DateTime endDate)
        {
            if (startDate >= endDate)
                return false; // Thời gian không hợp lệ

            var cleanupStart = startDate - _cleanupBuffer;
            var cleanupEnd = endDate + _cleanupBuffer;

            var conflictingBookings = await _context.DatPhongs
                .Where(dp => dp.MaPhong == maPhong
                          && dp.IsActive
                          && dp.TrangThai != "Hủy"
                          && dp.TrangThai != "Hoàn thành"
                          && (
                              (dp.NgayNhanPhong <= endDate && dp.NgayTraPhong >= startDate) ||
                              (dp.NgayNhanPhong <= startDate && dp.NgayTraPhong >= endDate) ||
                              (startDate < dp.NgayTraPhong + _cleanupBuffer && endDate > dp.NgayNhanPhong - _cleanupBuffer)
                          ))
                .AnyAsync();

            return !conflictingBookings;
        }
    }
}