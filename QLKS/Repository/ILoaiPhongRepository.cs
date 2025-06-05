using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLKS.Repository
{
    public interface ILoaiPhongRepository
    {
        Task<PagedLoaiPhongResponse> GetAllAsync(int pageNumber, int pageSize);
        Task<LoaiPhongMD> GetByIdAsync(int maLoaiPhong);
        Task<LoaiPhongMD> AddLoaiPhongAsync(LoaiPhongVM loaiPhongVM);
        Task<bool> EditLoaiPhongAsync(int maLoaiPhong, LoaiPhongVM loaiPhongVM);
        Task<bool> DeleteLoaiPhongAsync(int maLoaiPhong);
    }

    public class LoaiPhongRepository : ILoaiPhongRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public LoaiPhongRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }
        public async Task<PagedLoaiPhongResponse> GetAllAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.LoaiPhongs.AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var loaiPhongs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(lp => new LoaiPhongMD
                {
                    MaLoaiPhong = lp.MaLoaiPhong,
                    TenLoaiPhong = lp.TenLoaiPhong,
                    GiaCoBan = lp.GiaCoBan,
                    SoNguoiToiDa = lp.SoNguoiToiDa
                })
                .ToListAsync();

            return new PagedLoaiPhongResponse
            {
                LoaiPhongs = loaiPhongs,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<LoaiPhongMD> GetByIdAsync(int maLoaiPhong)
        {
            var loaiPhong = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.MaLoaiPhong == maLoaiPhong);

            if (loaiPhong == null)
            {
                return null; // Trả về null để controller xử lý lỗi
            }

            return new LoaiPhongMD
            {
                MaLoaiPhong = loaiPhong.MaLoaiPhong,
                TenLoaiPhong = loaiPhong.TenLoaiPhong,
                GiaCoBan = loaiPhong.GiaCoBan,
                SoNguoiToiDa = loaiPhong.SoNguoiToiDa
            };
        }

        public async Task<LoaiPhongMD> AddLoaiPhongAsync(LoaiPhongVM loaiPhongVM)
        {
            // Kiểm tra trùng TenLoaiPhong
            var check = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.TenLoaiPhong == loaiPhongVM.TenLoaiPhong);
            if (check != null)
            {
                throw new ArgumentException("Loại phòng đã tồn tại");
            }

            var loaiPhong = new LoaiPhong
            {
                TenLoaiPhong = loaiPhongVM.TenLoaiPhong,
                GiaCoBan = loaiPhongVM.GiaCoBan,
                SoNguoiToiDa = loaiPhongVM.SoNguoiToiDa
            };

            _context.LoaiPhongs.Add(loaiPhong);
            await _context.SaveChangesAsync();

            return new LoaiPhongMD
            {
                MaLoaiPhong = loaiPhong.MaLoaiPhong,
                TenLoaiPhong = loaiPhong.TenLoaiPhong,
                GiaCoBan = loaiPhong.GiaCoBan,
                SoNguoiToiDa = loaiPhong.SoNguoiToiDa
            };
        }

        public async Task<bool> EditLoaiPhongAsync(int maLoaiPhong, LoaiPhongVM loaiPhongVM)
        {
            var loaiPhong = await _context.LoaiPhongs
                .SingleOrDefaultAsync(lp => lp.MaLoaiPhong == maLoaiPhong);

            if (loaiPhong == null)
            {
                return false; // Trả về false để controller xử lý lỗi
            }

            // Kiểm tra trùng TenLoaiPhong với loại phòng khác
            var checkDuplicate = await _context.LoaiPhongs
                .FirstOrDefaultAsync(lp => lp.TenLoaiPhong == loaiPhongVM.TenLoaiPhong && lp.MaLoaiPhong != maLoaiPhong);
            if (checkDuplicate != null)
            {
                throw new ArgumentException("Tên loại phòng đã tồn tại");
            }

            loaiPhong.TenLoaiPhong = loaiPhongVM.TenLoaiPhong;
            loaiPhong.GiaCoBan = loaiPhongVM.GiaCoBan;
            loaiPhong.SoNguoiToiDa = loaiPhongVM.SoNguoiToiDa;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLoaiPhongAsync(int maLoaiPhong)
        {
            var loaiPhong = await _context.LoaiPhongs
                .SingleOrDefaultAsync(lp => lp.MaLoaiPhong == maLoaiPhong);

            if (loaiPhong == null)
            {
                return false; // Trả về false để controller xử lý lỗi
            }

            // Kiểm tra xem loại phòng có đang được sử dụng bởi phòng nào không
            var phongUsingLoaiPhong = await _context.Phongs
                .AnyAsync(p => p.MaLoaiPhong == maLoaiPhong);
            if (phongUsingLoaiPhong)
            {
                throw new ArgumentException("Không thể xóa loại phòng vì đang được sử dụng bởi ít nhất một phòng");
            }

            _context.LoaiPhongs.Remove(loaiPhong);
            await _context.SaveChangesAsync();
            return true;
        }
    
}
}