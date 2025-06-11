using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLKS.Repository
{
    public interface IDatPhongRepository
    {
        Task<PagedDatPhongResponse> GetAllVMAsync(int pageNumber, int pageSize);
        Task<DatPhongVM> GetByIdVMAsync(int maDatPhong);
        Task AddVMAsync(List<CreateDatPhongVM> datPhongVMs, List<int> maKhList);
        Task UpdateVMAsync(int maDatPhong, UpdateDatPhongVM datPhongVM);
        Task<bool> DeleteByMaDatPhongAsync(int maDatPhong);
        Task UpdateDatPhongTrangThaiByMaDatPhongAsync(int maDatPhong, string trangThai);
    }

    public class DatPhongRepository : IDatPhongRepository
    {
        private readonly DataQlks112Nhom3Context _context;
        private readonly TimeSpan _cleanupBuffer = TimeSpan.FromHours(2); // Khoảng dọn dẹp: 2 tiếng

        public DatPhongRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedDatPhongResponse> GetAllVMAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.DatPhongs
                .Where(dp => dp.IsActive == true)
                .Include(dp => dp.MaPhongNavigation)
                .Include(dp => dp.MaKhNavigation)
                .Include(dp => dp.SuDungDichVus);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var datPhongs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<DatPhongVM>();
            foreach (var dp in datPhongs)
            {
                var datPhongVM = MapToVM(dp);
                datPhongVM.DanhSachKhachHang = await _context.KhachHangs
                    .Where(kh => kh.MaDatPhong == dp.MaDatPhong && kh.IsActive == true)
                    .Select(kh => new TenKhachHangVM
                    {
                        HoTen = kh.HoTen
                    })
                    .ToListAsync();

                datPhongVM.DanhSachDichVu = await _context.SuDungDichVus
                    .Where(sddv => sddv.MaDatPhong == dp.MaDatPhong && sddv.IsActive == true)
                    .Join(_context.DichVus,
                          sddv => sddv.MaDichVu,
                          dv => dv.MaDichVu,
                          (sddv, dv) => new SuDungDichVuVM
                          {
                              MaSuDung = sddv.MaSuDung,
                              MaDatPhong = sddv.MaDatPhong ?? 0,
                              MaDichVu = sddv.MaDichVu ?? 0,
                              TenDichVu = dv.TenDichVu,
                              SoLuong = sddv.SoLuong,
                              ThanhTien = sddv.ThanhTien ?? 0
                          })
                    .ToListAsync();

                result.Add(datPhongVM);
            }

            return new PagedDatPhongResponse
            {
                DatPhongs = result,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<DatPhongVM> GetByIdVMAsync(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .Where(dp => dp.MaDatPhong == maDatPhong && dp.IsActive == true)
                .Include(dp => dp.MaPhongNavigation)
                .Include(dp => dp.MaKhNavigation)
                .FirstOrDefaultAsync();

            if (datPhong == null)
                return null;

            var datPhongVM = MapToVM(datPhong);
            datPhongVM.DanhSachKhachHang = await _context.KhachHangs
                .Where(kh => kh.MaDatPhong == datPhong.MaDatPhong && kh.IsActive == true)
                .Select(kh => new TenKhachHangVM
                {
                    HoTen = kh.HoTen
                })
                .ToListAsync();

            datPhongVM.DanhSachDichVu = await _context.SuDungDichVus
                .Where(sddv => sddv.MaDatPhong == datPhong.MaDatPhong && sddv.IsActive == true)
                .Join(_context.DichVus,
                      sddv => sddv.MaDichVu,
                      dv => dv.MaDichVu,
                      (sddv, dv) => new SuDungDichVuVM
                      {
                          MaSuDung = sddv.MaSuDung,
                          MaDatPhong = sddv.MaDatPhong ?? 0,
                          MaDichVu = sddv.MaDichVu ?? 0,
                          TenDichVu = dv.TenDichVu,
                          SoLuong = sddv.SoLuong,
                          ThanhTien = sddv.ThanhTien ?? 0
                      })
                .ToListAsync();

            datPhongVM.SoLuongDichVuSuDung = datPhongVM.DanhSachDichVu?.Count ?? 0;

            return datPhongVM;
        }

        public async Task AddVMAsync(List<CreateDatPhongVM> datPhongVMs, List<int> maKhList)
        {
            if (datPhongVMs == null || !datPhongVMs.Any())
                throw new ArgumentException("Danh sách đặt phòng không được để trống.");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    Console.WriteLine($"Bắt đầu thêm {datPhongVMs.Count} đặt phòng với maKhList: {string.Join(", ", maKhList)}");

                    if (maKhList == null || !maKhList.Any())
                        throw new ArgumentException("Danh sách khách hàng không được để trống.");

                    var khachHangs = await _context.KhachHangs
                        .Where(kh => maKhList.Contains(kh.MaKh) && kh.IsActive == true)
                        .ToListAsync();

                    Console.WriteLine($"Tìm thấy {khachHangs.Count} khách hàng hợp lệ.");

                    if (khachHangs.Count != maKhList.Count)
                        throw new ArgumentException("Một hoặc nhiều khách hàng trong danh sách không tồn tại hoặc đã bị ẩn.");

                    var datPhongs = datPhongVMs.Select(datPhongVM => new DatPhong
                    {
                        MaNv = datPhongVM.MaNv,
                        MaKh = datPhongVM.MaKh,
                        MaPhong = datPhongVM.MaPhong,
                        NgayDat = datPhongVM.NgayDat ?? DateOnly.FromDateTime(DateTime.Now),
                        NgayNhanPhong = datPhongVM.NgayNhanPhong,
                        NgayTraPhong = datPhongVM.NgayTraPhong,
                        SoNguoiO = datPhongVM.SoNguoiO,
                        TrangThai = datPhongVM.TrangThai?.Trim() ?? "Đã đặt",
                        IsActive = true
                    }).ToList();

                    foreach (var datPhong in datPhongs)
                    {
                        Console.WriteLine($"Xử lý DatPhong: MaPhong={datPhong.MaPhong}, NgayNhanPhong={datPhong.NgayNhanPhong}, NgayTraPhong={datPhong.NgayTraPhong}");

                        var phong = await _context.Phongs.FindAsync(datPhong.MaPhong);
                        if (phong?.TrangThai == "Bảo trì" && datPhong.TrangThai != "Hủy")
                            throw new ArgumentException($"Phòng {datPhong.MaPhong} đang bảo trì, không thể đặt trừ trạng thái 'Hủy'.");

                        if (await CheckBookingConflictAsync(datPhong.MaPhong, datPhong.NgayNhanPhong.Value, datPhong.NgayTraPhong.Value))
                            throw new ArgumentException($"Phòng {datPhong.MaPhong} đã được đặt trong khoảng thời gian từ {datPhong.NgayNhanPhong} đến {datPhong.NgayTraPhong} hoặc nằm trong khoảng dọn dẹp 2 tiếng.");

                        await ValidateSingleDatPhong(datPhong);

                        _context.DatPhongs.Add(datPhong);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Đã lưu DatPhong với MaDatPhong={datPhong.MaDatPhong}");

                        var khachHangsToUpdate = await _context.KhachHangs
                            .Where(kh => maKhList.Contains(kh.MaKh) && kh.IsActive == true)
                            .ToListAsync();
                        foreach (var khachHang in khachHangsToUpdate)
                        {
                            khachHang.MaDatPhong = datPhong.MaDatPhong;
                            _context.KhachHangs.Update(khachHang);
                            Console.WriteLine($"Cập nhật MaDatPhong={datPhong.MaDatPhong} cho KhachHang MaKh={khachHang.MaKh}");
                        }
                        await _context.SaveChangesAsync();

                        await UpdatePhongStatusAsync(datPhong.MaPhong);
                        Console.WriteLine($"Đã cập nhật trạng thái phòng {datPhong.MaPhong}");
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine("Giao dịch hoàn tất.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Lỗi khi thêm dữ liệu: {ex.Message} - Inner: {ex.InnerException?.Message}");
                    throw new Exception($"Lỗi khi thêm dữ liệu: {ex.Message}", ex);
                }
            }
        }

        public async Task UpdateVMAsync(int maDatPhong, UpdateDatPhongVM datPhongVM)
        {
            var existingDatPhong = await _context.DatPhongs
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong && dp.IsActive == true);

            if (existingDatPhong == null)
                throw new ArgumentException("Đặt phòng không tồn tại hoặc đã bị ẩn.");

            var originalMaPhong = existingDatPhong.MaPhong;
            var originalNgayNhanPhong = existingDatPhong.NgayNhanPhong;
            var originalNgayTraPhong = existingDatPhong.NgayTraPhong;

            existingDatPhong.MaNv = datPhongVM.MaNv ?? existingDatPhong.MaNv;
            existingDatPhong.MaKh = datPhongVM.MaKh ?? existingDatPhong.MaKh;
            existingDatPhong.MaPhong = datPhongVM.MaPhong ?? existingDatPhong.MaPhong;
            existingDatPhong.NgayDat = datPhongVM.NgayDat ?? existingDatPhong.NgayDat;
            existingDatPhong.NgayNhanPhong = datPhongVM.NgayNhanPhong ?? existingDatPhong.NgayNhanPhong;
            existingDatPhong.NgayTraPhong = datPhongVM.NgayTraPhong ?? existingDatPhong.NgayTraPhong;
            existingDatPhong.SoNguoiO = datPhongVM.SoNguoiO;
            existingDatPhong.TrangThai = datPhongVM.TrangThai?.Trim() ?? existingDatPhong.TrangThai;

            await ValidateSingleDatPhong(existingDatPhong);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.DatPhongs.Update(existingDatPhong);
                    await _context.SaveChangesAsync();

                    if (existingDatPhong.MaPhong != originalMaPhong ||
                        existingDatPhong.NgayNhanPhong != originalNgayNhanPhong ||
                        existingDatPhong.NgayTraPhong != originalNgayTraPhong)
                    {
                        if (await CheckBookingConflictAsync(existingDatPhong.MaPhong, existingDatPhong.NgayNhanPhong.Value, existingDatPhong.NgayTraPhong.Value, maDatPhong))
                            throw new ArgumentException($"Phòng {existingDatPhong.MaPhong} đã được đặt trong khoảng thời gian từ {existingDatPhong.NgayNhanPhong} đến {existingDatPhong.NgayTraPhong} hoặc nằm trong khoảng dọn dẹp 2 tiếng.");
                    }

                    // --- SỬA ĐOẠN NÀY ---
                    if (datPhongVM.MaKhList != null && datPhongVM.MaKhList.Any())
                    {
                        // 1. Lấy tất cả khách hàng cũ của đặt phòng này
                        var oldKhachHangs = await _context.KhachHangs
                            .Where(kh => kh.MaDatPhong == maDatPhong && kh.IsActive == true)
                            .ToListAsync();

                        // 2. Những khách hàng KHÔNG còn trong danh sách mới thì gán MaDatPhong = null
                        foreach (var oldKh in oldKhachHangs)
                        {
                            if (!datPhongVM.MaKhList.Contains(oldKh.MaKh))
                            {
                                oldKh.MaDatPhong = null;
                                _context.KhachHangs.Update(oldKh);
                            }
                        }

                        // 3. Cập nhật MaDatPhong cho các khách hàng mới
                        var newKhachHangs = await _context.KhachHangs
                            .Where(kh => datPhongVM.MaKhList.Contains(kh.MaKh) && kh.IsActive == true)
                            .ToListAsync();

                        if (newKhachHangs.Count != datPhongVM.MaKhList.Count)
                            throw new ArgumentException("Một hoặc nhiều khách hàng trong danh sách không tồn tại hoặc đã bị ẩn.");

                        foreach (var khachHang in newKhachHangs)
                        {
                            khachHang.MaDatPhong = maDatPhong;
                            _context.KhachHangs.Update(khachHang);
                        }
                        await _context.SaveChangesAsync();
                    }
                    // --- HẾT SỬA ---

                    await UpdatePhongStatusAsync(existingDatPhong.MaPhong);

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi cập nhật dữ liệu: {ex.Message}", ex);
                }
            }
        }

        public async Task<bool> DeleteByMaDatPhongAsync(int maDatPhong)
        {
            var datPhong = await _context.DatPhongs
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong && dp.IsActive == true);

            if (datPhong == null)
                return false;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    datPhong.IsActive = false;
                    _context.DatPhongs.Update(datPhong);
                    await _context.SaveChangesAsync();

                    await UpdatePhongStatusAsync(datPhong.MaPhong);
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi xóa dữ liệu: {ex.Message}", ex);
                }
            }
            return true;
        }

        public async Task UpdateDatPhongTrangThaiByMaDatPhongAsync(int maDatPhong, string trangThai)
        {
            if (string.IsNullOrEmpty(trangThai))
                throw new ArgumentException("Trạng thái không được để trống.");

            trangThai = trangThai.Trim();

            var validTrangThaiDatPhong = new[] { "Đang sử dụng", "Hủy", "Hoàn thành", "Đã đặt" };
            if (!validTrangThaiDatPhong.Contains(trangThai, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Trạng thái không hợp lệ. Chỉ cho phép: Đang sử dụng, Hủy, Hoàn thành, Đã đặt.");

            var booking = await _context.DatPhongs
                .FirstOrDefaultAsync(dp => dp.MaDatPhong == maDatPhong && dp.IsActive == true);

            if (booking == null)
                throw new ArgumentException($"Không tìm thấy đặt phòng với mã {maDatPhong} hoặc đã bị ẩn.");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    booking.TrangThai = trangThai;
                    _context.DatPhongs.Update(booking);
                    await _context.SaveChangesAsync();

                    await UpdatePhongStatusAsync(booking.MaPhong);

                    await transaction.CommitAsync();
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi cập nhật trạng thái đặt phòng: {ex.InnerException?.Message}", ex);
                }
            }
        }

        private async Task<bool> CheckBookingConflictAsync(string maPhong, DateTime ngayNhanPhong, DateTime ngayTraPhong, int? maDatPhongToExclude = null)
        {
            if (ngayNhanPhong >= ngayTraPhong)
                return true;

            var cleanupStart = ngayNhanPhong - _cleanupBuffer;
            var cleanupEnd = ngayTraPhong + _cleanupBuffer;

            var existingBookings = await _context.DatPhongs
                .Where(dp => dp.MaPhong == maPhong
                          && dp.IsActive
                          && dp.TrangThai != "Hủy"
                          && dp.TrangThai != "Hoàn thành"
                          && (maDatPhongToExclude == null || dp.MaDatPhong != maDatPhongToExclude))
                .ToListAsync();

            foreach (var booking in existingBookings)
            {
                var bookingCleanupStart = booking.NgayNhanPhong - _cleanupBuffer;
                var bookingCleanupEnd = booking.NgayTraPhong + _cleanupBuffer;

                if (
                    (ngayNhanPhong < booking.NgayTraPhong && ngayTraPhong > booking.NgayNhanPhong) ||
                    (ngayNhanPhong <= booking.NgayNhanPhong && ngayTraPhong >= booking.NgayTraPhong) ||
                    (booking.NgayNhanPhong <= ngayNhanPhong && booking.NgayTraPhong >= ngayTraPhong) ||
                    (ngayNhanPhong < bookingCleanupEnd && ngayTraPhong > bookingCleanupStart) ||
                    (cleanupStart < booking.NgayTraPhong && cleanupEnd > booking.NgayNhanPhong)
                )
                {
                    return true;
                }
            }
            return false;
        }

        private async Task UpdatePhongStatusAsync(string maPhong)
        {
            var phong = await _context.Phongs
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (phong == null || phong.TrangThai == "Bảo trì")
                return;

            var latestBooking = await _context.DatPhongs
                .Where(dp => dp.MaPhong == maPhong && dp.IsActive == true)
                .OrderByDescending(dp => dp.NgayDat)
                .FirstOrDefaultAsync();

            if (latestBooking != null)
            {
                phong.TrangThai = latestBooking.TrangThai switch
                {
                    "Đang sử dụng" => "Đang sử dụng",
                    "Hủy" or "Hoàn thành" => "Trống",
                    _ => "Trống"
                };
            }
            else
            {
                phong.TrangThai = "Trống";
            }

            await _context.SaveChangesAsync();
        }

        private async Task ValidateDatPhong(List<DatPhong> datPhongs)
        {
            if (datPhongs == null || !datPhongs.Any())
                throw new ArgumentException("Danh sách đặt phòng không được để trống.");

            var duplicatePhongs = datPhongs.GroupBy(dp => dp.MaPhong)
                                           .Where(g => g.Count() > 1)
                                           .Select(g => g.Key)
                                           .ToList();
            if (duplicatePhongs.Any())
                throw new ArgumentException($"Các phòng sau bị trùng lặp trong danh sách đặt: {string.Join(", ", duplicatePhongs)}");

            foreach (var datPhong in datPhongs)
            {
                await ValidateSingleDatPhong(datPhong);
            }
        }

        private async Task ValidateSingleDatPhong(DatPhong datPhong)
        {
            if (string.IsNullOrEmpty(datPhong.MaPhong))
                throw new ArgumentException("Mã phòng không được để trống.");

            if (datPhong.NgayNhanPhong == null || datPhong.NgayTraPhong == null)
                throw new ArgumentException("Ngày nhận phòng và ngày trả phòng không được để trống.");

            if (datPhong.NgayNhanPhong > datPhong.NgayTraPhong)
                throw new ArgumentException("Ngày nhận phòng phải trước ngày trả phòng.");

            if (datPhong.SoNguoiO <= 0)
                throw new ArgumentException("Số người ở phải lớn hơn 0.");

            var phong = await _context.Phongs.FindAsync(datPhong.MaPhong);
            if (phong == null)
                throw new ArgumentException($"Phòng {datPhong.MaPhong} không tồn tại.");

            if (phong.TrangThai == "Bảo trì")
                throw new ArgumentException($"Phòng {datPhong.MaPhong} đang bảo trì, không thể đặt.");

            if (datPhong.MaKh.HasValue)
            {
                var khachHang = await _context.KhachHangs.FindAsync(datPhong.MaKh);
                if (khachHang == null)
                    throw new ArgumentException("Khách hàng không tồn tại.");
            }

            if (datPhong.MaNv.HasValue)
            {
                var nhanVien = await _context.NhanViens.FindAsync(datPhong.MaNv);
                if (nhanVien == null)
                    throw new ArgumentException("Nhân viên không tồn tại.");
            }

            var validTrangThai = new[] { "Đang sử dụng", "Hủy", "Hoàn thành", "Đã đặt" };
            if (!string.IsNullOrEmpty(datPhong.TrangThai))
            {
                datPhong.TrangThai = datPhong.TrangThai.Trim();
                if (!validTrangThai.Contains(datPhong.TrangThai, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException("Trạng thái không hợp lệ. Chỉ cho phép: Đang sử dụng, Hủy, Đã đặt, Hoàn thành.");
            }
        }

        private DatPhongVM MapToVM(DatPhong datPhong)
        {
            return new DatPhongVM
            {
                MaDatPhong = datPhong.MaDatPhong,
                MaNv = datPhong.MaNv,
                MaKh = datPhong.MaKh,
                TenKhachHang = datPhong.MaKhNavigation?.HoTen,
                MaPhong = datPhong.MaPhong,
                NgayDat = datPhong.NgayDat ?? DateOnly.FromDateTime(DateTime.Now),
                NgayNhanPhong = datPhong.NgayNhanPhong,
                NgayTraPhong = datPhong.NgayTraPhong,
                SoNguoiO = datPhong.SoNguoiO,
                PhuThu = datPhong.PhuThu,
                TrangThai = datPhong.TrangThai,
                TongTienPhong = datPhong.TongTienPhong,
                SoLuongDichVuSuDung = datPhong.SuDungDichVus?.Count ?? 0,
                DanhSachKhachHang = new List<TenKhachHangVM>()
            };
        }
    }
}
