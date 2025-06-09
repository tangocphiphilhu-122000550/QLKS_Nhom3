using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;

namespace QLKS.Repository
{
    public interface IDichVuRepository
    {
        Task<PagedDichVuResponse> GetAllDichVu(int pageNumber, int pageSize);
        Task<List<DichVuVM>> GetDichVuByName(string tenDichVu);
        Task<DichVuVM> AddDichVu(DichVuVM dichVu);
        Task<bool> UpdateDichVu(string tenDichVu, DichVuVM dichVuVM); // Thay maDichVu bằng tenDichVu
        Task<bool> DeleteDichVu(string tenDichVu);
    }
    public class DichVuRepository : IDichVuRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public DichVuRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedDichVuResponse> GetAllDichVu(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.DichVus
                .AsNoTracking();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var dichVus = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(dv => new DichVuVM
                {
                    MaDichVu = dv.MaDichVu,
                    TenDichVu = dv.TenDichVu,
                    DonGia = dv.DonGia,
                    MoTa = dv.MoTa
                })
                .ToListAsync();

            return new PagedDichVuResponse
            {
                DichVus = dichVus,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<DichVuVM>> GetDichVuByName(string tenDichVu)
        {
            return await _context.DichVus
                .AsNoTracking()
                .Where(dv => dv.TenDichVu.Contains(tenDichVu))
                .Select(dv => new DichVuVM
                {
                    MaDichVu = dv.MaDichVu,
                    TenDichVu = dv.TenDichVu,
                    DonGia = dv.DonGia,
                    MoTa = dv.MoTa
                })
                .ToListAsync();
        }

        public async Task<DichVuVM> AddDichVu(DichVuVM dichVuVM)
        {
            if (string.IsNullOrEmpty(dichVuVM.TenDichVu) || dichVuVM.DonGia <= 0)
            {
                throw new ArgumentException("Tên dịch vụ và đơn giá không hợp lệ.");
            }

            var dichVu = new DichVu
            {
                TenDichVu = dichVuVM.TenDichVu,
                DonGia = dichVuVM.DonGia,
                MoTa = dichVuVM.MoTa
            };

            _context.DichVus.Add(dichVu);
            await _context.SaveChangesAsync();

            return new DichVuVM
            {
                TenDichVu = dichVu.TenDichVu,
                DonGia = dichVu.DonGia,
                MoTa = dichVu.MoTa
            };
        }

        public async Task<bool> UpdateDichVu(string tenDichVu, DichVuVM dichVuVM)
        {
            if (string.IsNullOrEmpty(dichVuVM.TenDichVu) || dichVuVM.DonGia <= 0)
            {
                throw new ArgumentException("Tên dịch vụ và đơn giá không hợp lệ.");
            }

            var existingDichVu = await _context.DichVus
                .AsNoTracking()
                .FirstOrDefaultAsync(dv => dv.TenDichVu == tenDichVu); // Tìm bằng TenDichVu
            if (existingDichVu == null)
            {
                return false;
            }

            existingDichVu.TenDichVu = dichVuVM.TenDichVu;
            existingDichVu.DonGia = dichVuVM.DonGia;
            existingDichVu.MoTa = dichVuVM.MoTa;

            _context.DichVus.Update(existingDichVu);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDichVu(string tenDichVu)
        {
            var dichVu = await _context.DichVus
                .AsNoTracking()
                .FirstOrDefaultAsync(dv => dv.TenDichVu == tenDichVu); // Tìm bằng TenDichVu
            if (dichVu == null)
            {
                return false;
            }

            _context.DichVus.Remove(dichVu);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
