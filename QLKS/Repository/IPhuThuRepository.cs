using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;

namespace QLKS.Repository
{
    public interface IPhuThuRepository
    {
        Task<PagedPhuThuResponse> GetAllPhuThu(int pageNumber, int pageSize);
        Task<List<PhuThuVM>> GetPhuThuByLoaiPhong(int maLoaiPhong);
        Task<PhuThuVM> AddPhuThu(PhuThuVM phuThu);
        Task<bool> UpdatePhuThu(int maPhuThu, PhuThuVM phuThuVM);
        Task<bool> DeletePhuThu(int maPhuThu);
    }

    public class PhuThuRepository : IPhuThuRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public PhuThuRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedPhuThuResponse> GetAllPhuThu(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.PhuThus
                .Include(pt => pt.MaLoaiPhongNavigation)
                .AsNoTracking();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var phuThus = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(pt => new PhuThuVM
                {
                    MaPhuThu = pt.MaPhuThu,
                    MaLoaiPhong = pt.MaLoaiPhong,
                    GiaPhuThuTheoNgay = pt.GiaPhuThuTheoNgay,
                    GiaPhuThuTheoGio = pt.GiaPhuThuTheoGio,
                    TenLoaiPhong = pt.MaLoaiPhongNavigation.TenLoaiPhong
                })
                .ToListAsync();

            return new PagedPhuThuResponse
            {
                PhuThus = phuThus,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<PhuThuVM>> GetPhuThuByLoaiPhong(int maLoaiPhong)
        {
            return await _context.PhuThus
                .Include(pt => pt.MaLoaiPhongNavigation)
                .AsNoTracking()
                .Where(pt => pt.MaLoaiPhong == maLoaiPhong)
                .Select(pt => new PhuThuVM
                {
                    MaPhuThu = pt.MaPhuThu,
                    MaLoaiPhong = pt.MaLoaiPhong,
                    GiaPhuThuTheoNgay = pt.GiaPhuThuTheoNgay,
                    GiaPhuThuTheoGio = pt.GiaPhuThuTheoGio,
                    TenLoaiPhong = pt.MaLoaiPhongNavigation.TenLoaiPhong
                })
                .ToListAsync();
        }

        public async Task<PhuThuVM> AddPhuThu(PhuThuVM phuThuVM)
        {
            // Validate input
            if (!phuThuVM.MaLoaiPhong.HasValue || 
                (!phuThuVM.GiaPhuThuTheoNgay.HasValue && !phuThuVM.GiaPhuThuTheoGio.HasValue))
            {
                throw new ArgumentException("Mã loại phòng và ít nhất một loại giá phụ thu là bắt buộc.");
            }

            // Validate if LoaiPhong exists
            var loaiPhong = await _context.LoaiPhongs.FindAsync(phuThuVM.MaLoaiPhong);
            if (loaiPhong == null)
            {
                throw new ArgumentException("Loại phòng không tồn tại.");
            }

            var phuThu = new PhuThu
            {
                MaLoaiPhong = phuThuVM.MaLoaiPhong,
                GiaPhuThuTheoNgay = phuThuVM.GiaPhuThuTheoNgay,
                GiaPhuThuTheoGio = phuThuVM.GiaPhuThuTheoGio
            };

            _context.PhuThus.Add(phuThu);
            await _context.SaveChangesAsync();

            phuThuVM.MaPhuThu = phuThu.MaPhuThu;
            phuThuVM.TenLoaiPhong = loaiPhong.TenLoaiPhong;
            return phuThuVM;
        }

        public async Task<bool> UpdatePhuThu(int maPhuThu, PhuThuVM phuThuVM)
        {
            if (!phuThuVM.MaLoaiPhong.HasValue || 
                (!phuThuVM.GiaPhuThuTheoNgay.HasValue && !phuThuVM.GiaPhuThuTheoGio.HasValue))
            {
                throw new ArgumentException("Mã loại phòng và ít nhất một loại giá phụ thu là bắt buộc.");
            }

            var existingPhuThu = await _context.PhuThus.FindAsync(maPhuThu);
            if (existingPhuThu == null)
            {
                return false;
            }

            // Validate if LoaiPhong exists
            var loaiPhong = await _context.LoaiPhongs.FindAsync(phuThuVM.MaLoaiPhong);
            if (loaiPhong == null)
            {
                throw new ArgumentException("Loại phòng không tồn tại.");
            }

            existingPhuThu.MaLoaiPhong = phuThuVM.MaLoaiPhong;
            existingPhuThu.GiaPhuThuTheoNgay = phuThuVM.GiaPhuThuTheoNgay;
            existingPhuThu.GiaPhuThuTheoGio = phuThuVM.GiaPhuThuTheoGio;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePhuThu(int maPhuThu)
        {
            var phuThu = await _context.PhuThus.FindAsync(maPhuThu);
            if (phuThu == null)
            {
                return false;
            }

            _context.PhuThus.Remove(phuThu);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 