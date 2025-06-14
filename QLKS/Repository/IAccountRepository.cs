using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLKS.Repository
{
    public interface IAccountRepository
    {
        Task<PagedAccountResponse> GetAllAccounts(int pageNumber, int pageSize);
        Task<List<NhanVien>> GetByNameNhanVien(string hoTen);
        Task<NhanVien> AddAccount(AddAccountDTO dto);
        Task<bool> UpdateAccount(string email, UpdateAccountDTO nhanVien);
        Task<bool> DeleteAccount(string email); // Giữ tên nhưng đổi thành vô hiệu hóa
        Task<bool> RestoreAccount(string email); // Thêm phương thức mới
    }

    public class AccountRepository : IAccountRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public AccountRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedAccountResponse> GetAllAccounts(int pageNumber, int pageSize)
        {
            // Đảm bảo pageNumber và pageSize hợp lệ
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            // Lấy toàn bộ nhân viên (không lọc IsActive)
            var query = _context.NhanViens.AsQueryable();

            // Tổng số bản ghi và tổng số trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy danh sách nhân viên cho trang hiện tại
            var accounts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Trả về dữ liệu đã phân trang và map sang DTO
            return new PagedAccountResponse
            {
                Accounts = accounts.Select(nv => new Account
                {
                    HoTen = nv.HoTen ?? "Không xác định",
                    MaVaiTro = nv.MaVaiTro,
                    SoDienThoai = nv.SoDienThoai,
                    Email = nv.Email,
                    GioiTinh = nv.GioiTinh,
                    DiaChi = nv.DiaChi,
                    NgaySinh = nv.NgaySinh,
                    HasPassword = nv.HasPassword ?? false,
                    IsActive = nv.IsActive
                }).ToList(),
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }


        public async Task<List<NhanVien>> GetByNameNhanVien(string hoTen)
        {
            return await _context.NhanViens
                .Include(nv => nv.MaVaiTroNavigation)
                .Where(nv => nv.HoTen.Contains(hoTen) && nv.IsActive) // Chỉ lấy nhân viên đang hoạt động
                .ToListAsync();
        }

        public async Task<NhanVien> AddAccount(AddAccountDTO dto)
        {
            var existingUser = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == dto.Email);
            if (existingUser != null)
                throw new Exception("Email đã được sử dụng.");

            var vaiTro = await _context.VaiTros
                .FirstOrDefaultAsync(vt => vt.MaVaiTro == dto.MaVaiTro);
            if (vaiTro == null)
                throw new Exception("Mã vai trò không tồn tại.");

            if (string.IsNullOrEmpty(dto.HoTen))
                throw new Exception("Họ tên là bắt buộc.");
            if (string.IsNullOrEmpty(dto.Email))
                throw new Exception("Email là bắt buộc.");

            var nhanVien = new NhanVien
            {
                HoTen = dto.HoTen,
                MaVaiTro = dto.MaVaiTro,
                SoDienThoai = dto.SoDienThoai,
                Email = dto.Email,
                GioiTinh = dto.GioiTinh,
                DiaChi = dto.DiaChi,
                NgaySinh = dto.NgaySinh,
                IsActive = true, 
                MatKhau = null,
                HasPassword = false 
            };

            try
            {
                _context.NhanViens.Add(nhanVien);
                await _context.SaveChangesAsync();
                return nhanVien;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Lỗi khi lưu dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }



        public async Task<bool> UpdateAccount(string email, UpdateAccountDTO nhanVien)
        {
            var existingNhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == email);
            if (existingNhanVien == null)
                return false;

            // Chỉ cập nhật những trường được gửi từ client
            if (nhanVien.HoTen != null)
                existingNhanVien.HoTen = nhanVien.HoTen;

            if (nhanVien.Email != null && nhanVien.Email != email)
            {
                var emailDuplicate = await _context.NhanViens
                    .FirstOrDefaultAsync(nv => nv.Email == nhanVien.Email);
                if (emailDuplicate != null)
                    throw new Exception("Email đã được sử dụng bởi tài khoản khác.");
                existingNhanVien.Email = nhanVien.Email;
            }

            if (nhanVien.SoDienThoai != null)
                existingNhanVien.SoDienThoai = nhanVien.SoDienThoai;

            if (nhanVien.MaVaiTro.HasValue)
            {
                var vaiTro = await _context.VaiTros
                    .FirstOrDefaultAsync(vt => vt.MaVaiTro == nhanVien.MaVaiTro.Value);
                if (vaiTro == null)
                    throw new Exception("Mã vai trò không tồn tại.");
                existingNhanVien.MaVaiTro = nhanVien.MaVaiTro;
            }

            if (nhanVien.GioiTinh != null)
                existingNhanVien.GioiTinh = nhanVien.GioiTinh;

            if (nhanVien.DiaChi != null)
                existingNhanVien.DiaChi = nhanVien.DiaChi;

            if (nhanVien.NgaySinh.HasValue)
                if (nhanVien.NgaySinh.HasValue)
                    existingNhanVien.NgaySinh = DateOnly.FromDateTime(nhanVien.NgaySinh.Value);
                

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAccount(string email)
        {
            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == email);
            if (nhanVien == null)
                return false;

            if (!nhanVien.IsActive)
                return false; // Đã vô hiệu hóa trước đó

            nhanVien.IsActive = false; // Vô hiệu hóa nhân viên
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAccount(string email)
        {
            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == email);
            if (nhanVien == null)
                return false;

            if (nhanVien.IsActive)
                return false; // Đã hoạt động, không cần khôi phục

            nhanVien.IsActive = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
