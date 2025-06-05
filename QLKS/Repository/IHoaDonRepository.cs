using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;
using iText.Layout.Properties;

namespace QLKS.Repository
{
    public interface IHoaDonRepository
    {
        Task<PagedHoaDonResponse> GetAllAsync(int pageNumber, int pageSize);
        Task<PagedHoaDonResponse> GetByTenKhachHangAsync(string tenKhachHang, int pageNumber, int pageSize);
        Task<HoaDonVM> CreateAsync(CreateHoaDonVM hoaDonVM);
        Task<bool> UpdateTrangThaiByTenKhachHangAsync(string tenKhachHang, UpdateHoaDonVM updateVM);
        Task<bool> UpdatePhuongThucThanhToanByTenKhachHangAsync(string tenKhachHang, UpdatePhuongThucThanhToanVM updateVM);
        Task<byte[]> ExportInvoicePdfAsync(int maHoaDon);
    }

    public class HoaDonRepository : IHoaDonRepository
    {
        private readonly DataQlks112Nhom3Context _context;

        public HoaDonRepository(DataQlks112Nhom3Context context)
        {
            _context = context;
        }

        public async Task<PagedHoaDonResponse> GetAllAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Include(hd => hd.MaNvNavigation)
                .Include(hd => hd.ChiTietHoaDons)
                    .ThenInclude(cthd => cthd.MaDatPhongNavigation)
                        .ThenInclude(dp => dp.SuDungDichVus)
                            .ThenInclude(sddv => sddv.MaDichVuNavigation);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var hoaDons = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedHoaDonResponse
            {
                HoaDons = hoaDons.Select(hd => MapToVM(hd)).ToList(),
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedHoaDonResponse> GetByTenKhachHangAsync(string tenKhachHang, int pageNumber, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(tenKhachHang))
                throw new ArgumentException("Tên khách hàng không được để trống.");

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Include(hd => hd.MaNvNavigation)
                .Include(hd => hd.ChiTietHoaDons)
                    .ThenInclude(cthd => cthd.MaDatPhongNavigation)
                        .ThenInclude(dp => dp.SuDungDichVus)
                            .ThenInclude(sddv => sddv.MaDichVuNavigation)
                .Where(hd => hd.MaKhNavigation.HoTen.Contains(tenKhachHang));

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var hoaDons = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedHoaDonResponse
            {
                HoaDons = hoaDons.Select(hd => MapToVM(hd)).ToList(),
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<HoaDonVM> CreateAsync(CreateHoaDonVM hoaDonVM)
        {
            if (hoaDonVM == null)
                throw new ArgumentNullException(nameof(hoaDonVM));

            if (string.IsNullOrWhiteSpace(hoaDonVM.HoTenKhachHang))
                throw new ArgumentException("Họ tên khách hàng không được để trống.");

            if (string.IsNullOrWhiteSpace(hoaDonVM.HoTenNhanVien))
                throw new ArgumentException("Họ tên nhân viên không được để trống.");

            if (hoaDonVM.MaDatPhongs == null || !hoaDonVM.MaDatPhongs.Any())
                throw new ArgumentException("Danh sách đặt phòng không được để trống.");

            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.HoTen == hoaDonVM.HoTenKhachHang);
            if (khachHang == null)
                throw new ArgumentException($"Không tìm thấy khách hàng với họ tên: {hoaDonVM.HoTenKhachHang}");

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.HoTen == hoaDonVM.HoTenNhanVien);
            if (nhanVien == null)
                throw new ArgumentException($"Không tìm thấy nhân viên với họ tên: {hoaDonVM.HoTenNhanVien}");

            string trangThai = hoaDonVM.TrangThai?.Trim();
            Console.WriteLine($"TrangThai trước chuẩn hóa: '{hoaDonVM.TrangThai}'");

            if (string.IsNullOrEmpty(trangThai))
            {
                trangThai = "Chưa thanh toán";
            }
            else
            {
                trangThai = trangThai.Normalize(NormalizationForm.FormC).ToLower().Trim();
                trangThai = trangThai switch
                {
                    "chưa thanh toán" => "Chưa thanh toán",
                    "đã thanh toán trước" => "Đã thanh toán trước",
                    "đã thanh toán" => "Đã thanh toán",
                    _ => throw new ArgumentException($"Trạng thái không hợp lệ: '{hoaDonVM.TrangThai}'. Chỉ cho phép: Chưa thanh toán, Đã thanh toán trước, Đã thanh toán.")
                };
            }
            Console.WriteLine($"TrangThai sau chuẩn hóa: '{trangThai}'");

            var hoaDon = new HoaDon
            {
                MaKh = khachHang.MaKh,
                MaNv = nhanVien.MaNv,
                NgayLap = hoaDonVM.NgayLap ?? DateOnly.FromDateTime(DateTime.Now),
                PhuongThucThanhToan = hoaDonVM.PhuongThucThanhToan,
                TrangThai = trangThai,
                TongTien = 0 // Trigger sẽ cập nhật
            };

            await ValidateHoaDon(hoaDon);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Lưu HoaDon trước để lấy MaHoaDon
                    _context.HoaDons.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    // Thêm ChiTietHoaDon với TongTienPhong và TongTienDichVu
                    var maDatPhongs = hoaDonVM.MaDatPhongs;
                    var datPhongs = await _context.DatPhongs
                        .Include(dp => dp.SuDungDichVus)
                        .Include(dp => dp.MaPhongNavigation)
                            .ThenInclude(p => p.MaLoaiPhongNavigation)
                        .Where(dp => maDatPhongs.Contains(dp.MaDatPhong))
                        .ToListAsync();

                    foreach (var maDatPhong in maDatPhongs)
                    {
                        var datPhong = datPhongs.FirstOrDefault(dp => dp.MaDatPhong == maDatPhong);
                        if (datPhong == null)
                            throw new ArgumentException($"Đặt phòng với MaDatPhong {maDatPhong} không tồn tại.");

                        var existingChiTiet = await _context.ChiTietHoaDons
                            .AnyAsync(cthd => cthd.MaDatPhong == maDatPhong);
                        if (existingChiTiet)
                            throw new ArgumentException($"Đặt phòng với MaDatPhong {maDatPhong} đã được gán cho hóa đơn khác.");

                        var tongTienPhong = datPhong.TongTienPhong ?? 0; // Giá phòng + phụ thu
                        var tongTienDichVu = datPhong.SuDungDichVus?.Sum(sddv => sddv.ThanhTien ?? 0) ?? 0;

                        hoaDon.ChiTietHoaDons.Add(new ChiTietHoaDon
                        {
                            MaHoaDon = hoaDon.MaHoaDon,
                            MaDatPhong = maDatPhong,
                            TongTienPhong = tongTienPhong,
                            TongTienDichVu = tongTienDichVu
                        });
                    }
                    await _context.SaveChangesAsync();

                    if (hoaDon.TrangThai == "Đã thanh toán")
                    {
                        foreach (var datPhong in datPhongs)
                        {
                            datPhong.TrangThai = "Hoàn thành";
                            datPhong.IsActive = false;
                        }

                        var phongIds = datPhongs.Select(dp => dp.MaPhong).Distinct().ToList();
                        foreach (var phongId in phongIds)
                        {
                            var phong = await _context.Phongs.FindAsync(phongId);
                            if (phong != null)
                            {
                                phong.TrangThai = "Trống";
                            }
                        }

                        var suDungDichVus = await _context.SuDungDichVus
                            .Where(sddv => maDatPhongs.Contains(sddv.MaDatPhong.Value))
                            .ToListAsync();
                        foreach (var sddv in suDungDichVus)
                        {
                            sddv.IsActive = false;
                        }

                        var khachHangToUpdate = await _context.KhachHangs
                            .FirstOrDefaultAsync(kh => kh.MaKh == hoaDon.MaKh);
                        if (khachHangToUpdate != null)
                        {
                            khachHangToUpdate.IsActive = false;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Lỗi khi lưu hóa đơn: {ex.Message} - Inner: {ex.InnerException?.Message}");
                    throw new Exception($"Lỗi khi tạo hóa đơn: {ex.Message} - Inner: {ex.InnerException?.Message}", ex);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Lỗi khi tạo hóa đơn: {ex.Message} - Inner: {ex.InnerException?.Message}");
                    throw new Exception($"Lỗi khi tạo hóa đơn: {ex.Message} - Inner: {ex.InnerException?.Message}", ex);
                }
            }

            var newHoaDon = await _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Include(hd => hd.MaNvNavigation)
                .Include(hd => hd.ChiTietHoaDons)
                    .ThenInclude(cthd => cthd.MaDatPhongNavigation)
                        .ThenInclude(dp => dp.SuDungDichVus)
                            .ThenInclude(sddv => sddv.MaDichVuNavigation)
                .FirstOrDefaultAsync(hd => hd.MaHoaDon == hoaDon.MaHoaDon);

            if (newHoaDon == null)
                throw new Exception("Không thể truy xuất hóa đơn vừa tạo.");

            return MapToVM(newHoaDon);
        }

        public async Task<bool> UpdateTrangThaiByTenKhachHangAsync(string tenKhachHang, UpdateHoaDonVM updateVM)
        {
            if (string.IsNullOrWhiteSpace(tenKhachHang))
                throw new ArgumentException("Tên khách hàng không được để trống.");

            if (updateVM == null || string.IsNullOrWhiteSpace(updateVM.TrangThai))
                throw new ArgumentException("Trạng thái không được để trống.");

            string trangThai = updateVM.TrangThai?.Trim();
            Console.WriteLine($"TrangThai trước chuẩn hóa: '{updateVM.TrangThai}'");
            if (string.IsNullOrEmpty(trangThai))
            {
                trangThai = "Chưa thanh toán";
            }
            else
            {
                trangThai = trangThai.Normalize(NormalizationForm.FormC).ToLower().Trim();
                trangThai = trangThai switch
                {
                    "chưa thanh toán" => "Chưa thanh toán",
                    "đã thanh toán trước" => "Đã thanh toán trước",
                    "đã thanh toán" => "Đã thanh toán",
                    _ => throw new ArgumentException($"Trạng thái không hợp lệ: '{updateVM.TrangThai}'. Chỉ cho phép: Chưa thanh toán, Đã thanh toán trước, Đã thanh toán.")
                };
            }
            Console.WriteLine($"TrangThai sau chuẩn hóa: '{trangThai}'");

            var hoaDons = await _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Include(hd => hd.ChiTietHoaDons)
                .Where(hd => hd.MaKhNavigation.HoTen.Contains(tenKhachHang))
                .ToListAsync();

            if (!hoaDons.Any())
                return false;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var hoaDon in hoaDons)
                    {
                        hoaDon.TrangThai = trangThai;
                        hoaDon.TongTien = 0; // Trigger sẽ cập nhật

                        if (trangThai == "Đã thanh toán")
                        {
                            var maDatPhongs = hoaDon.ChiTietHoaDons.Select(cthd => cthd.MaDatPhong).ToList();
                            var datPhongs = await _context.DatPhongs
                                .Where(dp => maDatPhongs.Contains(dp.MaDatPhong))
                                .ToListAsync();

                            foreach (var datPhong in datPhongs)
                            {
                                datPhong.IsActive = false;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi cập nhật trạng thái: {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> UpdatePhuongThucThanhToanByTenKhachHangAsync(string tenKhachHang, UpdatePhuongThucThanhToanVM updateVM)
        {
            if (string.IsNullOrWhiteSpace(tenKhachHang))
                throw new ArgumentException("Tên khách hàng không được để trống.");

            if (updateVM == null || string.IsNullOrWhiteSpace(updateVM.PhuongThucThanhToan))
                throw new ArgumentException("Phương thức thanh toán không được để trống.");

            var validPhuongThucThanhToan = new[] { "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng" };
            if (!validPhuongThucThanhToan.Contains(updateVM.PhuongThucThanhToan, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Phương thức thanh toán không hợp lệ. Chỉ cho phép: Tiền mặt, Chuyển khoản, Thẻ tín dụng.");

            var hoaDons = await _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Where(hd => hd.MaKhNavigation.HoTen.Contains(tenKhachHang))
                .ToListAsync();

            if (!hoaDons.Any())
                return false;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var hoaDon in hoaDons)
                    {
                        hoaDon.PhuongThucThanhToan = updateVM.PhuongThucThanhToan;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi cập nhật phương thức thanh toán: {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<byte[]> ExportInvoicePdfAsync(int maHoaDon)
        {
            var hoaDon = await _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Include(hd => hd.MaNvNavigation)
                .Include(hd => hd.ChiTietHoaDons)
                    .ThenInclude(cthd => cthd.MaDatPhongNavigation)
                        .ThenInclude(dp => dp.SuDungDichVus)
                            .ThenInclude(sddv => sddv.MaDichVuNavigation)
                .FirstOrDefaultAsync(hd => hd.MaHoaDon == maHoaDon);

            if (hoaDon == null)
                throw new ArgumentException($"Không tìm thấy hóa đơn với mã: {maHoaDon}");

            return GenerateInvoicePdf(hoaDon);
        }

        private byte[] GenerateInvoicePdf(HoaDon hoaDon)
        {
            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4);
                document.SetMargins(36, 36, 36, 50); // Căn lề chuẩn cho tài liệu chuyên nghiệp

                // Explicitly specify the namespace for 'Path' to resolve ambiguity
                string fontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Fonts", "arial.ttf");
                PdfFont font;
                try
                {
                    Console.WriteLine($"Đang tải font từ: {fontPath}");
                    if (!File.Exists(fontPath))
                    {
                        throw new FileNotFoundException($"Font file không tồn tại tại: {fontPath}");
                    }
                    PdfFontFactory.Register(fontPath);
                    font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tải font: {ex.Message}. Sử dụng font mặc định.");
                    font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                }

                // Màu sắc
                Color headerBgColor = new DeviceRgb(230, 240, 250); // Xanh dương nhạt
                Color tableHeaderColor = new DeviceRgb(50, 50, 50); // Xám đậm
                Color tableRowColor = new DeviceRgb(245, 245, 245); // Xám nhạt cho hàng xen kẽ

                // Explicitly specify the namespace for 'Path' to resolve ambiguity
                string logoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources", "logo.png");
                if (File.Exists(logoPath))
                {
                    Image logo = new Image(ImageDataFactory.Create(logoPath));
                    logo.SetWidth(100); // Điều chỉnh kích thước logo
                    logo.SetHorizontalAlignment(HorizontalAlignment.LEFT);
                    document.Add(logo);
                }

                Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth();
                headerTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("KHÁCH SẠN HOÀNG GIA")
                        .SetFont(font)
                        .SetFontSize(16)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.LEFT)));
                headerTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("HÓA ĐƠN THANH TOÁN")
                        .SetFont(font)
                        .SetFontSize(18)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.RIGHT)));
                headerTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("123 Đường Hoàng Gia, Quận 1, TP.HCM\nĐiện thoại: (028) 1234 5678\nEmail: contact@hoanggiahotel.com")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.LEFT)));
                headerTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph($"Mã hóa đơn: {hoaDon.MaHoaDon}\nNgày lập: {hoaDon.NgayLap?.ToString("dd/MM/yyyy") ?? "Chưa xác định"}")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.RIGHT)));
                document.Add(headerTable);

                // Đường kẻ ngang phân cách
                document.Add(new LineSeparator(new SolidLine(1f))
                    .SetMarginTop(5)
                    .SetMarginBottom(10));

                // Thông tin khách hàng và nhân viên
                Table infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetMarginBottom(15);
                infoTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph($"Khách hàng: {hoaDon.MaKhNavigation?.HoTen ?? "Không có thông tin"}")
                        .SetFont(font)
                        .SetFontSize(11)));
                infoTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph($"Nhân viên: {hoaDon.MaNvNavigation?.HoTen ?? "Không có thông tin"}")
                        .SetFont(font)
                        .SetFontSize(11)
                        .SetTextAlignment(TextAlignment.RIGHT)));
                infoTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph($"Phương thức thanh toán: {hoaDon.PhuongThucThanhToan ?? "Chưa xác định"}")
                        .SetFont(font)
                        .SetFontSize(11)));
                infoTable.AddCell(new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph($"Trạng thái: {hoaDon.TrangThai ?? "Chưa xác định"}")
                        .SetFont(font)
                        .SetFontSize(11)
                        .SetTextAlignment(TextAlignment.RIGHT)));
                document.Add(infoTable);

                // Chi tiết hóa đơn
                document.Add(new Paragraph("CHI TIẾT HÓA ĐƠN")
                    .SetFont(font)
                    .SetFontSize(14)
                    .SetBold()
                    .SetBackgroundColor(headerBgColor)
                    .SetPadding(5)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(10)
                    .SetMarginBottom(10));

                int index = 1;
                foreach (var chiTiet in hoaDon.ChiTietHoaDons)
                {
                    var datPhong = chiTiet.MaDatPhongNavigation;

                    // Thông tin phòng
                    document.Add(new Paragraph($"Phòng: {datPhong?.MaPhong ?? "N/A"}")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetBold()
                        .SetMarginTop(10));

                    Table roomTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                        .UseAllAvailableWidth();
                    string soNgayO = "Không xác định";
                    if (datPhong?.NgayNhanPhong.HasValue == true && datPhong?.NgayTraPhong.HasValue == true)
                    {
                        var soNgay = (datPhong.NgayTraPhong.Value - datPhong.NgayNhanPhong.Value).Days;
                        soNgayO = soNgay.ToString();
                    }
                    roomTable.AddCell(new Cell()
                        .SetBorder(Border.NO_BORDER)
                        .Add(new Paragraph($"Số ngày ở: {soNgayO}")
                            .SetFont(font)
                            .SetFontSize(11)));
                    roomTable.AddCell(new Cell()
                        .SetBorder(Border.NO_BORDER)
                        .Add(new Paragraph($"Tiền phòng: {datPhong?.TongTienPhong?.ToString("N0") ?? "0"} VNĐ")
                            .SetFont(font)
                            .SetFontSize(11)));
                    roomTable.AddCell(new Cell()
                        .SetBorder(Border.NO_BORDER)
                        .Add(new Paragraph($"Phụ thu: {datPhong?.PhuThu?.ToString("N0") ?? "0"} VNĐ")
                            .SetFont(font)
                            .SetFontSize(11)));
                    document.Add(roomTable);

                    // Dịch vụ sử dụng
                    if (datPhong?.SuDungDichVus?.Any() == true)
                    {
                        document.Add(new Paragraph("Dịch vụ đã sử dụng:")
                            .SetFont(font)
                            .SetFontSize(11)
                            .SetBold()
                            .SetMarginTop(5));

                        Table dichVuTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 20, 15, 20, 20, 15 }))
                            .UseAllAvailableWidth();

                        // Tiêu đề bảng dịch vụ
                        dichVuTable.AddHeaderCell(new Cell()
                            .SetBackgroundColor(tableHeaderColor)
                            .SetFontColor(ColorConstants.WHITE)
                            .Add(new Paragraph("STT")
                                .SetFont(font)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.CENTER)));
                        dichVuTable.AddHeaderCell(new Cell()
                            .SetBackgroundColor(tableHeaderColor)
                            .SetFontColor(ColorConstants.WHITE)
                            .Add(new Paragraph("Tên dịch vụ")
                                .SetFont(font)
                                .SetFontSize(10)));
                        dichVuTable.AddHeaderCell(new Cell()
                            .SetBackgroundColor(tableHeaderColor)
                            .SetFontColor(ColorConstants.WHITE)
                            .Add(new Paragraph("Số lượng")
                                .SetFont(font)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.CENTER)));
                        dichVuTable.AddHeaderCell(new Cell()
                            .SetBackgroundColor(tableHeaderColor)
                            .SetFontColor(ColorConstants.WHITE)
                            .Add(new Paragraph("Ngày sử dụng")
                                .SetFont(font)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.CENTER)));
                        dichVuTable.AddHeaderCell(new Cell()
                            .SetBackgroundColor(tableHeaderColor)
                            .SetFontColor(ColorConstants.WHITE)
                            .Add(new Paragraph("Ngày kết thúc")
                                .SetFont(font)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.CENTER)));
                        dichVuTable.AddHeaderCell(new Cell()
                            .SetBackgroundColor(tableHeaderColor)
                            .SetFontColor(ColorConstants.WHITE)
                            .Add(new Paragraph("Thành tiền")
                                .SetFont(font)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.RIGHT)));

                        // Dữ liệu dịch vụ
                        int dichVuIndex = 1;
                        foreach (var suDungDichVu in datPhong.SuDungDichVus)
                        {
                            dichVuTable.AddCell(new Cell()
                                .SetBackgroundColor(dichVuIndex % 2 == 0 ? tableRowColor : ColorConstants.WHITE)
                                .Add(new Paragraph(dichVuIndex.ToString())
                                    .SetFont(font)
                                    .SetFontSize(10)
                                    .SetTextAlignment(TextAlignment.CENTER)));
                            dichVuTable.AddCell(new Cell()
                                .SetBackgroundColor(dichVuIndex % 2 == 0 ? tableRowColor : ColorConstants.WHITE)
                                .Add(new Paragraph(suDungDichVu.MaDichVuNavigation?.TenDichVu ?? "N/A")
                                    .SetFont(font)
                                    .SetFontSize(10)));
                            dichVuTable.AddCell(new Cell()
                                .SetBackgroundColor(dichVuIndex % 2 == 0 ? tableRowColor : ColorConstants.WHITE)
                                    .Add(new Paragraph(suDungDichVu.SoLuong.ToString())
                                    .SetFont(font)
                                    .SetFontSize(10)
                                    .SetTextAlignment(TextAlignment.CENTER)));
                            dichVuTable.AddCell(new Cell()
                                .SetBackgroundColor(dichVuIndex % 2 == 0 ? tableRowColor : ColorConstants.WHITE)
                                .Add(new Paragraph(suDungDichVu.NgaySuDung?.ToString("dd/MM/yyyy") ?? "N/A")
                                    .SetFont(font)
                                    .SetFontSize(10)
                                    .SetTextAlignment(TextAlignment.CENTER)));
                            dichVuTable.AddCell(new Cell()
                                .SetBackgroundColor(dichVuIndex % 2 == 0 ? tableRowColor : ColorConstants.WHITE)
                                .Add(new Paragraph(suDungDichVu.NgayKetThuc?.ToString("dd/MM/yyyy") ?? "N/A")
                                    .SetFont(font)
                                    .SetFontSize(10)
                                    .SetTextAlignment(TextAlignment.CENTER)));
                            dichVuTable.AddCell(new Cell()
                                .SetBackgroundColor(dichVuIndex % 2 == 0 ? tableRowColor : ColorConstants.WHITE)
                                .Add(new Paragraph($"{suDungDichVu.ThanhTien?.ToString("N0") ?? "0"} VNĐ")
                                    .SetFont(font)
                                    .SetFontSize(10)
                                    .SetTextAlignment(TextAlignment.RIGHT)));
                            dichVuIndex++;
                        }

                        document.Add(dichVuTable);
                    }
                    else
                    {
                        document.Add(new Paragraph("Không sử dụng dịch vụ.")
                            .SetFont(font)
                            .SetFontSize(11)
                            .SetItalic()
                            .SetMarginBottom(10));
                    }

                    index++;
                }

                // Tổng tiền
                document.Add(new Paragraph($"Tổng tiền: {hoaDon.TongTien?.ToString("N0") ?? "Chưa tính"} VNĐ")
                    .SetFont(font)
                    .SetFontSize(14)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginTop(20));

                // Footer: Thêm thông tin liên hệ ở cuối mỗi trang
                pdf.AddEventHandler(PdfDocumentEvent.END_PAGE, new TextFooterEventHandler(font));

                document.Close();
                return memoryStream.ToArray();
            }
        }

        // Class để thêm footer vào mỗi trang
        private class TextFooterEventHandler : IEventHandler
        {
            private readonly PdfFont font;

            public TextFooterEventHandler(PdfFont font)
            {
                this.font = font;
            }

            public void HandleEvent(Event @event)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)@event;
                PdfDocument pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();
                PdfCanvas pdfCanvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);
                Canvas canvas = new Canvas(pdfCanvas, new Rectangle(36, 20, page.GetPageSize().GetWidth() - 72, 30));
                canvas.Add(new Paragraph("Cảm ơn Quý Khách đã sử dụng dịch vụ tại Khách Sạn Hoàng Gia! Liên hệ: (028) 1234 5678 - contact@hoanggiahotel.com")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.CENTER));
                canvas.Close();
            }
        }

        private async Task<decimal> TinhTongTien(HoaDon hoaDon)
        {
            return hoaDon.ChiTietHoaDons.Sum(cthd => (cthd.TongTienPhong ?? 0) + (cthd.TongTienDichVu ?? 0));
        }

        private async Task ValidateHoaDon(HoaDon hoaDon)
        {
            var validPhuongThucThanhToan = new[] { "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng" };
            if (!string.IsNullOrEmpty(hoaDon.PhuongThucThanhToan) && !validPhuongThucThanhToan.Contains(hoaDon.PhuongThucThanhToan, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Phương thức thanh toán không hợp lệ. Chỉ cho phép: Tiền mặt, Chuyển khoản, Thẻ tín dụng.");
        }

        private HoaDonVM MapToVM(HoaDon hd)
        {
            return new HoaDonVM
            {
                MaHoaDon = hd.MaHoaDon,
                TenKhachHang = hd.MaKhNavigation?.HoTen,
                TenNhanVien = hd.MaNvNavigation?.HoTen,
                NgayLap = hd.NgayLap,
                TongTien = hd.TongTien,
                PhuongThucThanhToan = hd.PhuongThucThanhToan,
                TrangThai = hd.TrangThai,
                ChiTietHoaDons = hd.ChiTietHoaDons?.Select(cthd =>
                {
                    return new ChiTietHoaDonVM
                    {
                        MaPhong = cthd.MaDatPhongNavigation?.MaPhong,
                        TongTienPhong = cthd.TongTienPhong,
                        PhuThu = cthd.MaDatPhongNavigation?.PhuThu,
                        TongTienDichVu = cthd.TongTienDichVu,
                        SoNguoiO = cthd.MaDatPhongNavigation?.SoNguoiO,
                        NgayNhanPhong = cthd.MaDatPhongNavigation?.NgayNhanPhong.HasValue == true
                            ? cthd.MaDatPhongNavigation.NgayNhanPhong.Value
                            : (DateTime?)null,
                        NgayTraPhong = cthd.MaDatPhongNavigation?.NgayTraPhong.HasValue == true
                            ? cthd.MaDatPhongNavigation.NgayTraPhong.Value
                            : (DateTime?)null,
                        DanhSachDichVu = cthd.MaDatPhongNavigation?.SuDungDichVus?.Select(sddv =>
                        {
                            return new SuDungDichVuMD
                            {
                                TenDichVu = sddv.MaDichVuNavigation?.TenDichVu,
                                SoLuong = sddv.SoLuong,
                                NgaySuDung = sddv.NgaySuDung.HasValue ? sddv.NgaySuDung.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                                NgayKetThuc = sddv.NgayKetThuc.HasValue ? sddv.NgayKetThuc.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                                ThanhTien = sddv.ThanhTien
                            };
                        }).ToList() ?? new List<SuDungDichVuMD>()
                    };
                }).ToList() ?? new List<ChiTietHoaDonVM>()
            };
        }
    }
}