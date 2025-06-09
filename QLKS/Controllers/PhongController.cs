using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QLKS.Models;
using QLKS.Repository;
using System;
using System.Threading.Tasks;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PhongController : ControllerBase
    {
        private readonly IPhongRepository _phong;

        public PhongController(IPhongRepository phong)
        {
            _phong = phong;
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var phong = await _phong.GetAllAsync(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách phòng thành công!",
                    data = phong
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

         [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet("{MaPhong}")]
        public async Task<IActionResult> GetById(string MaPhong)
        {
            try
            {
                var result = await _phong.GetByIdAsync(MaPhong);
                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phòng với mã này.",
                        data = (object)null
                    });
                }
                return Ok(new
                {
                    message = "Lấy thông tin phòng thành công!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy thông tin phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpPost]
        public async Task<IActionResult> AddPhong([FromBody] PhongAddVM phongVM)
        {
            if (phongVM == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu phòng không được để trống.",
                    data = (object)null
                });
            }

            try
            {
                var result = await _phong.AddPhongAsync(phongVM);
                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = "Thêm phòng thành công!",
                    data = result
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
                    message = "Lỗi khi thêm phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy,NhanVien")]
        [HttpPut("{MaPhong}")]
        public async Task<IActionResult> EditPhong(string MaPhong, [FromBody] PhongEditVM phongVM)
        {
            if (phongVM == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu phòng không được để trống.",
                    data = (object)null
                });
            }

            try
            {
                var result = await _phong.EditPhongAsync(MaPhong, phongVM);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phòng để cập nhật.",
                        data = (object)null
                    });
                }
                return Ok(new
                {
                    message = "Cập nhật phòng thành công!",
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
                    message = "Lỗi khi cập nhật phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpDelete("{MaPhong}")]
        public async Task<IActionResult> DeletePhong(string MaPhong)
        {
            try
            {
                var result = await _phong.DeletePhongAsync(MaPhong);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phòng để xóa.",
                        data = (object)null
                    });
                }
                return Ok(new
                {
                    message = "Xóa phòng thành công!",
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
                    message = "Lỗi khi xóa phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet("trang-thai/{trangThai}")]
        public async Task<IActionResult> GetByTrangThai(string trangThai, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(trangThai))
            {
                return BadRequest(new
                {
                    message = "TrangThai không thể bỏ trống.",
                    data = (object)null
                });
            }

            try
            {
                var phongList = await _phong.GetByTrangThaiAsync(trangThai, pageNumber, pageSize);
                if (phongList.Phongs == null || !phongList.Phongs.Any())
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phòng nào với trạng thái được chỉ định.",
                        data = (object)null
                    });
                }
                return Ok(new
                {
                    message = "Lấy danh sách phòng theo trạng thái thành công!",
                    data = phongList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách phòng theo trạng thái: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPut("{maPhong}/trang-thai")]
        public async Task<IActionResult> UpdateTrangThai(string maPhong, [FromQuery] string trangThai)
        {
            if (string.IsNullOrEmpty(trangThai))
            {
                return BadRequest(new
                {
                    message = "TrangThai không thể bỏ trống.",
                    data = (object)null
                });
            }

            try
            {
                var result = await _phong.UpdateTrangThaiAsync(maPhong, trangThai);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phòng để cập nhật trạng thái.",
                        data = (object)null
                    });
                }
                return Ok(new
                {
                    message = "Cập nhật trạng thái phòng thành công!",
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
                    message = "Lỗi khi cập nhật trạng thái phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet("loai-phong/{maLoaiPhong}")]
        public async Task<IActionResult> GetByLoaiPhong(int maLoaiPhong, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var phongList = await _phong.GetByLoaiPhongAsync(maLoaiPhong, pageNumber, pageSize);
                if (phongList.Phongs == null || !phongList.Phongs.Any())
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phòng có mã loại phòng đã nhập.",
                        data = (object)null
                    });
                }
                return Ok(new
                {
                    message = "Lấy danh sách phòng theo loại phòng thành công!",
                    data = phongList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách phòng theo loại phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy,NhanVien")]
        [HttpGet("thong-ke-trang-thai")]
        public async Task<IActionResult> GetRoomStatusStatistics()
        {
            try
            {
                var statistics = await _phong.GetRoomStatusStatisticsAsync();
                return Ok(new
                {
                    message = "Lấy thống kê trạng thái phòng thành công!",
                    data = statistics
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy thống kê trạng thái phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

         [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet("{maPhong}/trang-thai-dat-phong")]
        public async Task<IActionResult> IsRoomAvailable(string maPhong, DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest(new
                {
                    message = "Ngày bắt đầu phải trước ngày kết thúc.",
                    data = (object)null
                });
            }

            try
            {
                var phong = await _phong.GetByIdAsync(maPhong);
                if (phong == null)
                {
                    return NotFound(new
                    {
                        message = "Phòng không tồn tại.",
                        data = (object)null
                    });
                }

                var isAvailable = await _phong.IsRoomAvailableAsync(maPhong, startDate, endDate);
                var trangThai = isAvailable ? "Phòng trống" : "Phòng đã được đặt";

                return Ok(new
                {
                    message = "Kiểm tra trạng thái đặt phòng thành công!",
                    data = new { MaPhong = maPhong, TrangThai = trangThai }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi kiểm tra trạng thái đặt phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }
}
