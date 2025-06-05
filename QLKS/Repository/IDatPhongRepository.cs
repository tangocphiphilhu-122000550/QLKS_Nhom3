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
        Task UpdateDatPhongTrangThaiByMaPhongAsync(string maPhong, string trangThai);
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
                .Include(dp => dp.SuDungDichVus); // Thêm Include để tải SuDungDichVus

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

            // Gán SoLuongDichVuSuDung dựa trên DanhSachDichVu
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
                        MaKh = datPhongVM.MaKh, // Lưu khách hàng đại diện
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

                        // Kiểm tra phòng bảo trì
                        var phong = await _context.Phongs.FindAsync(datPhong.MaPhong);
                        if (phong?.TrangThai == "Bảo trì" && datPhong.TrangThai != "Hủy")
                            throw new ArgumentException($"Phòng {datPhong.MaPhong} đang bảo trì, không thể đặt trừ trạng thái 'Hủy'.");

                        // Kiểm tra xung đột thời gian
                        if (await CheckBookingConflictAsync(datPhong.MaPhong, datPhong.NgayNhanPhong.Value, datPhong.NgayTraPhong.Value))
                            throw new ArgumentException($"Phòng {datPhong.MaPhong} đã được đặt trong khoảng thời gian từ {datPhong.NgayNhanPhong} đến {datPhong.NgayTraPhong} hoặc nằm trong khoảng dọn dẹp 2 tiếng.");

                        await ValidateSingleDatPhong(datPhong);

                        // Thêm bản ghi
                        _context.DatPhongs.Add(datPhong);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Đã lưu DatPhong với MaDatPhong={datPhong.MaDatPhong}");

                        // Cập nhật MaDatPhong cho khách hàng
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

                        // Cập nhật trạng thái phòng
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
                    // Cập nhật bản ghi DatPhong
                    _context.DatPhongs.Update(existingDatPhong);
                    await _context.SaveChangesAsync();

                    // Kiểm tra xung đột nếu thay đổi thời gian hoặc phòng
                    if (existingDatPhong.MaPhong != originalMaPhong ||
                        existingDatPhong.NgayNhanPhong != originalNgayNhanPhong ||
                        existingDatPhong.NgayTraPhong != originalNgayTraPhong)
                    {
                        if (await CheckBookingConflictAsync(existingDatPhong.MaPhong, existingDatPhong.NgayNhanPhong.Value, existingDatPhong.NgayTraPhong.Value, maDatPhong))
                            throw new ArgumentException($"Phòng {existingDatPhong.MaPhong} đã được đặt trong khoảng thời gian từ {existingDatPhong.NgayNhanPhong} đến {existingDatPhong.NgayTraPhong} hoặc nằm trong khoảng dọn dẹp 2 tiếng.");
                    }

                    // Xử lý danh sách khách hàng mới nếu có
                    if (datPhongVM.MaKhList != null && datPhongVM.MaKhList.Any())
                    {
                        var khachHangs = await _context.KhachHangs
                            .Where(kh => datPhongVM.MaKhList.Contains(kh.MaKh) && kh.IsActive == true)
                            .ToListAsync();

                        if (khachHangs.Count != datPhongVM.MaKhList.Count)
                            throw new ArgumentException("Một hoặc nhiều khách hàng trong danh sách không tồn tại hoặc đã bị ẩn.");

                        foreach (var khachHang in khachHangs)
                        {
                            khachHang.MaDatPhong = maDatPhong; // Gán MaDatPhong cho khách hàng mới
                            _context.KhachHangs.Update(khachHang);
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Cập nhật trạng thái phòng
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

        public async Task UpdateDatPhongTrangThaiByMaPhongAsync(string maPhong, string trangThai)
        {
            if (string.IsNullOrEmpty(maPhong))
                throw new ArgumentException("Mã phòng không được để trống.");

            if (string.IsNullOrEmpty(trangThai))
                throw new ArgumentException("Trạng thái không được để trống.");

            trangThai = trangThai.Trim();

            var validTrangThaiDatPhong = new[] { "Đang sử dụng", "Hủy", "Hoàn thành", "Đã đặt" };
            if (!validTrangThaiDatPhong.Contains(trangThai, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Trạng thái không hợp lệ. Chỉ cho phép: Đang sử dụng, Hủy, Hoàn thành, Đã đặt.");

            var relatedBookings = await _context.DatPhongs
                .Where(dp => dp.MaPhong == maPhong && dp.IsActive == true && dp.TrangThai != "Hủy")
                .ToListAsync();

            if (!relatedBookings.Any())
                throw new ArgumentException($"Không tìm thấy đặt phòng hợp lệ nào cho phòng {maPhong} để cập nhật trạng thái.");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var booking in relatedBookings)
                    {
                        booking.TrangThai = trangThai;
                        _context.DatPhongs.Update(booking);
                    }
                    await _context.SaveChangesAsync();

                    // Cập nhật trạng thái phòng
                    await UpdatePhongStatusAsync(maPhong);

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
                return true; // Xung đột: Thời gian không hợp lệ

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
                    // Xung đột trực tiếp
                    (ngayNhanPhong < booking.NgayTraPhong && ngayTraPhong > booking.NgayNhanPhong) ||
                    // Đặt phòng mới bao quanh đặt phòng hiện có
                    (ngayNhanPhong <= booking.NgayNhanPhong && ngayTraPhong >= booking.NgayTraPhong) ||
                    // Đặt phòng hiện có bao quanh đặt phòng mới
                    (booking.NgayNhanPhong <= ngayNhanPhong && booking.NgayTraPhong >= ngayTraPhong) ||
                    // Xung đột với khoảng dọn dẹp
                    (ngayNhanPhong < bookingCleanupEnd && ngayTraPhong > bookingCleanupStart) ||
                    (cleanupStart < booking.NgayTraPhong && cleanupEnd > booking.NgayNhanPhong)
                )
                {
                    return true; // Có xung đột
                }
            }
            return false; // Không có xung đột
        }

        private async Task UpdatePhongStatusAsync(string maPhong)
        {
            var phong = await _context.Phongs
                .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

            if (phong == null || phong.TrangThai == "Bảo trì")
                return; // Không cập nhật nếu phòng không tồn tại hoặc đang bảo trì

            var currentTime = DateTime.Now; // 17:14 PM +07, 21/5/2025

            // Kiểm tra đặt phòng hiện tại (Đang sử dụng)
            var currentBooking = await _context.DatPhongs
                .Where(dp => dp.MaPhong == maPhong
                          && dp.IsActive
                          && dp.TrangThai == "Đang sử dụng"
                          && currentTime >= dp.NgayNhanPhong
                          && currentTime <= dp.NgayTraPhong)
                .OrderBy(dp => dp.NgayNhanPhong)
                .FirstOrDefaultAsync();

            if (currentBooking != null)
            {
                phong.TrangThai = "Đang sử dụng";
                await _context.SaveChangesAsync();
                return;
            }

            // Kiểm tra đặt phòng trong tương lai (Đã đặt)
            var nextBooking = await _context.DatPhongs
                .Where(dp => dp.MaPhong == maPhong
                          && dp.IsActive
                          && dp.TrangThai == "Đã đặt"
                          && dp.NgayNhanPhong > currentTime)
                .OrderBy(dp => dp.NgayNhanPhong)
                .FirstOrDefaultAsync();

            if (nextBooking != null)
            {
                var tomorrow = currentTime.Date.AddDays(1);
                if (nextBooking.NgayNhanPhong.HasValue && nextBooking.NgayNhanPhong.Value.Date == tomorrow)
                {
                    phong.TrangThai = "Đã đặt";
                }
                else
                {
                    phong.TrangThai = "Trống";
                }
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