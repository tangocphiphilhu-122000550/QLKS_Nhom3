using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    public class HoaDonVM
    {
        public int MaHoaDon { get; set; }
        public string TenKhachHang { get; set; }
        public string TenNhanVien { get; set; }
        public DateOnly? NgayLap { get; set; }
        public decimal? TongTien { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string TrangThai { get; set; }
        public List<ChiTietHoaDonVM> ChiTietHoaDons { get; set; }
    }

    public class PagedHoaDonResponse
    {
        public List<HoaDonVM> HoaDons { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

    public class CreateHoaDonVM
    {
        public string HoTenKhachHang { get; set; }
        public string HoTenNhanVien { get; set; }
        public DateOnly? NgayLap { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string TrangThai { get; set; }
        public List<int> MaDatPhongs { get; set; }
    }

    public class UpdateHoaDonVM
    {
        public string TrangThai { get; set; }
    }

    public class UpdatePhuongThucThanhToanVM
    {
        public string PhuongThucThanhToan { get; set; }
    }

    public class ChiTietHoaDonVM
    {
        public string MaPhong { get; set; }
        public decimal? TongTienPhong { get; set; }
        public decimal? PhuThu { get; set; }
        public decimal? TongTienDichVu { get; set; }
        public int? SoNguoiO { get; set; }
        public DateTime? NgayNhanPhong { get; set; }
        public DateTime? NgayTraPhong { get; set; }
        public List<SuDungDichVuMD> DanhSachDichVu { get; set; }
    }

    public class SuDungDichVuMD
    {
        public string TenDichVu { get; set; }
        public int? SoLuong { get; set; }
        public DateTime? NgaySuDung { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public decimal? ThanhTien { get; set; }
    }

    // Model cho xuất PDF về máy (không cần email)
    public class ExportHoaDonRequest
    {
        public int MaHoaDon { get; set; }
    }

    // Model cho gửi PDF qua email (yêu cầu email)
    public class ExportHoaDonWithEmailRequest
    {
        public int MaHoaDon { get; set; }

        [Required(ErrorMessage = "The Email field is required.")]
        public string Email { get; set; }
    }
}