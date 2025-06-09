using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKS.Models;
using QLKS.Repository;
using QLKS.Helpers;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HoaDonController : ControllerBase
    {
        private readonly IHoaDonRepository _hoaDonRepository;
        private readonly EmailHelper _emailHelper;

        public HoaDonController(IHoaDonRepository hoaDonRepository, EmailHelper emailHelper)
        {
            _hoaDonRepository = hoaDonRepository;
            _emailHelper = emailHelper;
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet]
        public async Task<ActionResult<PagedHoaDonResponse>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var response = await _hoaDonRepository.GetAllAsync(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách hóa đơn thành công!",
                    data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Lỗi server: {ex.Message} - Inner: {ex.InnerException?.Message}",
                    data = (object)null
                });
            }
        }

         [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet("khach-hang/{tenKhachHang}")]
        public async Task<ActionResult<PagedHoaDonResponse>> GetByTenKhachHang(string tenKhachHang, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var response = await _hoaDonRepository.GetByTenKhachHangAsync(tenKhachHang, pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách hóa đơn theo tên khách hàng thành công!",
                    data = response
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Lỗi server: {ex.Message} - Inner: {ex.InnerException?.Message}",
                    data = (object)null
                });
            }
        }

         [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPost]
        public async Task<ActionResult<HoaDonVM>> Create([FromBody] CreateHoaDonVM hoaDonVM)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ.",
                    data = ModelState
                });

            try
            {
                var newHoaDon = await _hoaDonRepository.CreateAsync(hoaDonVM);
                return CreatedAtAction(nameof(GetAll), new { maHoaDon = newHoaDon.MaHoaDon }, new
                {
                    message = "Tạo hóa đơn thành công!",
                    data = newHoaDon
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Lỗi server: {ex.Message} - Inner: {ex.InnerException?.Message}",
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPost("{maHoaDon}/export-pdf")]
        public async Task<IActionResult> ExportPdf([FromBody] ExportHoaDonRequest request)
        {
            if (request == null || request.MaHoaDon <= 0)
            {
                return BadRequest(new
                {
                    message = "MaHoaDon là trường bắt buộc và phải lớn hơn 0.",
                    data = (object)null
                });
            }

            try
            {
                var pdfData = await _hoaDonRepository.ExportInvoicePdfAsync(request.MaHoaDon);
                var fileName = $"HoaDon_{request.MaHoaDon}.pdf";

                // Trả về file PDF để tải về máy
                Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");
                return File(pdfData, "application/pdf", fileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Lỗi server: {ex.Message} - Inner: {ex.InnerException?.Message}",
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPost("{maHoaDon}/export-pdf/email")]
        public async Task<IActionResult> ExportPdfWithEmail([FromBody] ExportHoaDonWithEmailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ.",
                    data = ModelState
                });

            try
            {
                var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(request.Email, emailRegex))
                    return BadRequest(new
                    {
                        message = "Email không hợp lệ.",
                        data = (object)null
                    });

                var pdfData = await _hoaDonRepository.ExportInvoicePdfAsync(request.MaHoaDon);
                var fileName = $"HoaDon_{request.MaHoaDon}.pdf";

                var subject = $"Hóa Đơn Thanh Toán - Mã {request.MaHoaDon}";
                var body = $"Kính gửi Quý Khách,\n\nĐính kèm là hóa đơn thanh toán (Mã: {request.MaHoaDon}) từ Khách Sạn Hoàng Gia.\nVui lòng xem chi tiết về phòng và các dịch vụ đã sử dụng trong file PDF đính kèm.\nCảm ơn Quý Khách đã sử dụng dịch vụ của chúng tôi!\n\nTrân trọng,\nKhách Sạn Hoàng Gia";
                await _emailHelper.SendEmailAsync(request.Email, subject, body, isHtml: false, attachmentData: pdfData, attachmentName: fileName);
                return Ok(new
                {
                    message = $"Hóa đơn đã được gửi đến {request.Email}.",
                    data = (object)null
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Lỗi server: {ex.Message} - Inner: {ex.InnerException?.Message}",
                    data = (object)null
                });
            }
        }

         [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPut("{maHoaDon}/trang-thai")]
        public async Task<IActionResult> UpdateTrangThaiByTenKhachHang(string tenKhachHang, [FromBody] UpdateHoaDonVM updateVM)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ.",
                    data = ModelState
                });

            try
            {
                var result = await _hoaDonRepository.UpdateTrangThaiByTenKhachHangAsync(tenKhachHang, updateVM);
                if (!result)
                    return NotFound(new
                    {
                        message = $"Không tìm thấy hóa đơn nào cho tên khách hàng: {tenKhachHang}",
                        data = (object)null
                    });

                return Ok(new
                {
                    message = "Cập nhật trạng thái hóa đơn thành công!",
                    data = (object)null
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Lỗi server: {ex.Message} - Inner: {ex.InnerException?.Message}",
                    data = (object)null
                });
            }
        }

         [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPut("{maHoaDon}/phuong-thuc-thanh-toan")]
        public async Task<IActionResult> UpdatePhuongThucThanhToanByTenKhachHang(string tenKhachHang, [FromBody] UpdatePhuongThucThanhToanVM updateVM)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Dữ liệu không hợp lệ.",
                    data = ModelState
                });

            try
            {
                var result = await _hoaDonRepository.UpdatePhuongThucThanhToanByTenKhachHangAsync(tenKhachHang, updateVM);
                if (!result)
                    return NotFound(new
                    {
                        message = $"Không tìm thấy hóa đơn nào cho tên khách hàng: {tenKhachHang}",
                        data = (object)null
                    });

                return Ok(new
                {
                    message = "Cập nhật phương thức thanh toán thành công!",
                    data = (object)null
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Lỗi server: {ex.Message} - Inner: {ex.InnerException?.Message}",
                    data = (object)null
                });
            }
        }
    }
}
