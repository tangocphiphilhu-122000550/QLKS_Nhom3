using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using QLKS.Models;
using QLKS.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DatPhongController : ControllerBase
    {
        private readonly IDatPhongRepository _datPhongRepository;

        public DatPhongController(IDatPhongRepository datPhongRepository)
        {
            _datPhongRepository = datPhongRepository;
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet]
        public async Task<ActionResult<PagedDatPhongResponse>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _datPhongRepository.GetAllVMAsync(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách đặt phòng thành công!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = $"Lỗi server: {ex.Message}",
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien")]
        [HttpGet("{maDatPhong}")]
        public async Task<ActionResult<DatPhongVM>> GetById(int maDatPhong)
        {
            try
            {
                var datPhong = await _datPhongRepository.GetByIdVMAsync(maDatPhong);
                if (datPhong == null)
                    return NotFound(new
                    {
                        message = "Đặt phòng không tồn tại.",
                        data = (object)null
                    });

                return Ok(new
                {
                    message = "Lấy thông tin đặt phòng thành công!",
                    data = datPhong
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = $"Lỗi server: {ex.Message}",
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateDatPhongRequest request)
        {
            if (request == null || request.DatPhongVMs == null || !request.DatPhongVMs.Any())
                return BadRequest(new
                {
                    message = "Danh sách đặt phòng không được để trống.",
                    data = (object)null
                });

            try
            {
                await _datPhongRepository.AddVMAsync(request.DatPhongVMs, request.MaKhList);
                return Ok(new
                {
                    message = "Thêm đặt phòng thành công!",
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
                return BadRequest(new
                {
                    message = $"Lỗi server: {ex.Message}",
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien")]
        [HttpPut("{maDatPhong}")]
        public async Task<ActionResult> Update(int maDatPhong, [FromBody] UpdateDatPhongVM datPhongVM)
        {
            if (datPhongVM == null)
                return BadRequest(new
                {
                    message = "Dữ liệu cập nhật không được để trống.",
                    data = (object)null
                });

            try
            {
                await _datPhongRepository.UpdateVMAsync(maDatPhong, datPhongVM);
                return Ok(new
                {
                    message = "Cập nhật đặt phòng thành công!",
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
                return BadRequest(new
                {
                    message = $"Lỗi server: {ex.Message}",
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpDelete("{maDatPhong}")]
        public async Task<ActionResult> Delete(int maDatPhong)
        {
            try
            {
                var result = await _datPhongRepository.DeleteByMaDatPhongAsync(maDatPhong);
                if (!result)
                    return NotFound(new
                    {
                        message = "Đặt phòng không tồn tại hoặc đã bị xóa.",
                        data = (object)null
                    });

                return Ok(new
                {
                    message = "Xóa đặt phòng thành công!",
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = $"Lỗi server: {ex.Message}",
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPut("bookings/{maDatPhong}/status")]
        public async Task<ActionResult> UpdateDatPhongTrangThai([FromRoute] int maDatPhong, [FromBody] string trangThai)
        {
            if (maDatPhong <= 0)
                return BadRequest(new
                {
                    message = "Mã đặt phòng không hợp lệ.",
                    data = (object)null
                });

            if (string.IsNullOrEmpty(trangThai))
                return BadRequest(new
                {
                    message = "Trạng thái không được để trống.",
                    data = (object)null
                });

            try
            {
                await _datPhongRepository.UpdateDatPhongTrangThaiByMaDatPhongAsync(maDatPhong, trangThai);
                return Ok(new
                {
                    message = $"Cập nhật trạng thái đặt phòng mã {maDatPhong} thành '{trangThai}' thành công!",
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
                    message = $"Lỗi server: {ex.Message}",
                    data = (object)null
                });
            }
        }
    }

    // Class để nhận dữ liệu từ request khi tạo đặt phòng
    public class CreateDatPhongRequest
    {
        public List<CreateDatPhongVM> DatPhongVMs { get; set; }
        public List<int> MaKhList { get; set; }
    }
}
