using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System;

namespace QLKS.Repository
{
    public interface ISuDungDichVuRepository
    {
        Task<PagedSuDungDichVuResponse> GetAllSuDungDichVu(int pageNumber, int pageSize);
        Task<bool> AddSuDungDichVu(CreateSuDungDichVuVM suDungDichVuVM); // Thay đổi kiểu trả về
        Task<bool> UpdateSuDungDichVu(int maSuDung, SuDungDichVuVM suDungDichVuVM);
        Task<bool> DeleteSuDungDichVu(int maSuDung);
    }

    public class SuDungDichVuRepository : ISuDungDichVuRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public SuDungDichVuRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedSuDungDichVuResponse> GetAllSuDungDichVu(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.SuDungDichVus
                .AsNoTracking()
                .Include(sddv => sddv.MaDichVuNavigation)
                .Where(sddv => sddv.IsActive == true); 

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var suDungDichVus = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(sddv => new SuDungDichVuVM
                {
                    MaSuDung = sddv.MaSuDung,
                    MaDatPhong = sddv.MaDatPhong,
                    MaDichVu = sddv.MaDichVu,
                    TenDichVu = sddv.MaDichVuNavigation.TenDichVu,
                    SoLuong = sddv.SoLuong,
                    NgaySuDung = sddv.NgaySuDung.HasValue ? sddv.NgaySuDung.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    NgayKetThuc = sddv.NgayKetThuc.HasValue ? sddv.NgayKetThuc.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    ThanhTien = sddv.ThanhTien
                })
                .ToListAsync();

            return new PagedSuDungDichVuResponse
            {
                SuDungDichVus = suDungDichVus,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<bool> AddSuDungDichVu(CreateSuDungDichVuVM suDungDichVuVM)
        {
            if (suDungDichVuVM.MaDatPhong == null || suDungDichVuVM.MaDichVu == null || suDungDichVuVM.SoLuong <= 0)
            {
                throw new ArgumentException("Mã đặt phòng, mã dịch vụ và số lượng không hợp lệ.");
            }

            if (suDungDichVuVM.NgaySuDung == null)
            {
                throw new ArgumentException("Ngày sử dụng không được để trống.");
            }

            if (suDungDichVuVM.NgayKetThuc.HasValue && suDungDichVuVM.NgayKetThuc < suDungDichVuVM.NgaySuDung)
            {
                throw new ArgumentException("Ngày kết thúc phải lớn hơn hoặc bằng ngày sử dụng.");
            }

            var datPhong = await _context.DatPhongs.FindAsync(suDungDichVuVM.MaDatPhong);
            if (datPhong == null)
            {
                throw new ArgumentException("Mã đặt phòng không tồn tại.");
            }

            var dichVu = await _context.DichVus.FindAsync(suDungDichVuVM.MaDichVu);
            if (dichVu == null)
            {
                throw new ArgumentException("Mã dịch vụ không tồn tại.");
            }

            var suDungDichVu = new QLKS.Data.SuDungDichVu
            {
                MaDatPhong = suDungDichVuVM.MaDatPhong,
                MaDichVu = suDungDichVuVM.MaDichVu,
                SoLuong = suDungDichVuVM.SoLuong,
                NgaySuDung = DateOnly.FromDateTime(suDungDichVuVM.NgaySuDung.Value),
                NgayKetThuc = suDungDichVuVM.NgayKetThuc.HasValue ? DateOnly.FromDateTime(suDungDichVuVM.NgayKetThuc.Value) : (DateOnly?)null,
                ThanhTien = null // Để trigger tự động tính toán
            };

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Sử dụng ExecuteSqlRawAsync để INSERT
                    var sqlInsert = @"
                        INSERT INTO dbo.SuDungDichVu (MaDatPhong, MaDichVu, SoLuong, NgaySuDung, NgayKetThuc)
                        VALUES ({0}, {1}, {2}, {3}, {4});";

                    await _context.Database.ExecuteSqlRawAsync(sqlInsert,
                        suDungDichVu.MaDatPhong,
                        suDungDichVu.MaDichVu,
                        suDungDichVu.SoLuong,
                        suDungDichVu.NgaySuDung,
                        suDungDichVu.NgayKetThuc);

                    // Trigger trg_SuDungDichVu_Insert sẽ tự động tính ThanhTien

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi thêm dữ liệu: {ex.Message}", ex);
                }
            }

            return true; // Trả về true để báo tạo thành công
        }

        public async Task<bool> UpdateSuDungDichVu(int maSuDung, SuDungDichVuVM suDungDichVuVM)
        {
            if (suDungDichVuVM.MaDatPhong == null || suDungDichVuVM.MaDichVu == null || suDungDichVuVM.SoLuong <= 0)
            {
                throw new ArgumentException("Mã đặt phòng, mã dịch vụ và số lượng không hợp lệ.");
            }

            if (suDungDichVuVM.NgaySuDung == null)
            {
                throw new ArgumentException("Ngày sử dụng không được để trống.");
            }

            if (suDungDichVuVM.NgayKetThuc.HasValue && suDungDichVuVM.NgayKetThuc < suDungDichVuVM.NgaySuDung)
            {
                throw new ArgumentException("Ngày kết thúc phải lớn hơn hoặc bằng ngày sử dụng.");
            }

            var existingSuDungDichVu = await _context.SuDungDichVus
                .FirstOrDefaultAsync(sddv => sddv.MaSuDung == maSuDung);
            if (existingSuDungDichVu == null)
            {
                return false;
            }

            var datPhong = await _context.DatPhongs.FindAsync(suDungDichVuVM.MaDatPhong);
            if (datPhong == null)
            {
                throw new ArgumentException("Mã đặt phòng không tồn tại.");
            }

            var dichVu = await _context.DichVus.FindAsync(suDungDichVuVM.MaDichVu);
            if (dichVu == null)
            {
                throw new ArgumentException("Mã dịch vụ không tồn tại.");
            }

            existingSuDungDichVu.MaDatPhong = suDungDichVuVM.MaDatPhong;
            existingSuDungDichVu.MaDichVu = suDungDichVuVM.MaDichVu;
            existingSuDungDichVu.SoLuong = suDungDichVuVM.SoLuong;
            existingSuDungDichVu.NgaySuDung = DateOnly.FromDateTime(suDungDichVuVM.NgaySuDung.Value);
            existingSuDungDichVu.NgayKetThuc = suDungDichVuVM.NgayKetThuc.HasValue ? DateOnly.FromDateTime(suDungDichVuVM.NgayKetThuc.Value) : null;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var sqlUpdate = @"
                        UPDATE dbo.SuDungDichVu
                        SET MaDatPhong = {0}, MaDichVu = {1}, SoLuong = {2}, NgaySuDung = {3}, NgayKetThuc = {4}
                        WHERE MaSuDung = {5};";

                    await _context.Database.ExecuteSqlRawAsync(sqlUpdate,
                        existingSuDungDichVu.MaDatPhong,
                        existingSuDungDichVu.MaDichVu,
                        existingSuDungDichVu.SoLuong,
                        existingSuDungDichVu.NgaySuDung,
                        existingSuDungDichVu.NgayKetThuc,
                        maSuDung);

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi cập nhật dữ liệu: {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> DeleteSuDungDichVu(int maSuDung)
        {
            var suDungDichVu = await _context.SuDungDichVus
                .FirstOrDefaultAsync(sddv => sddv.MaSuDung == maSuDung);
            if (suDungDichVu == null)
            {
                return false;
            }

            _context.SuDungDichVus.Remove(suDungDichVu);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}