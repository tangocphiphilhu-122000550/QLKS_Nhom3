using Microsoft.AspNetCore.Mvc;
using QLKS.Models;
using QLKS.Repository;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoaiPhongController : ControllerBase
    {
        private readonly ILoaiPhongRepository _loaiPhongRepository;

        public LoaiPhongController(ILoaiPhongRepository loaiPhongRepository)
        {
            _loaiPhongRepository = loaiPhongRepository;
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var loaiPhongs = await _loaiPhongRepository.GetAllAsync(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách loại phòng thành công!",
                    data = loaiPhongs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách loại phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien")]
        [HttpGet("{maLoaiPhong}")]
        public async Task<IActionResult> GetById(int maLoaiPhong)
        {
            try
            {
                var result = await _loaiPhongRepository.GetByIdAsync(maLoaiPhong);
                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy loại phòng với mã này.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Lấy thông tin loại phòng thành công!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy thông tin loại phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpPost]
        public async Task<IActionResult> AddLoaiPhong([FromBody] LoaiPhongVM loaiPhongVM)
        {
            if (loaiPhongVM == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu loại phòng không được để trống.",
                    data = (object)null
                });
            }

            try
            {
                var result = await _loaiPhongRepository.AddLoaiPhongAsync(loaiPhongVM);
                return Ok(new
                {
                    message = "Thêm loại phòng thành công!",
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
                    message = "Lỗi khi thêm loại phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy,NhanVien")]
        [HttpPut("{maLoaiPhong}")]
        public async Task<IActionResult> EditLoaiPhong(int maLoaiPhong, [FromBody] LoaiPhongVM loaiPhongVM)
        {
            if (loaiPhongVM == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu loại phòng không được để trống.",
                    data = (object)null
                });
            }

            try
            {
                var result = await _loaiPhongRepository.EditLoaiPhongAsync(maLoaiPhong, loaiPhongVM);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy loại phòng để cập nhật.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Cập nhật loại phòng thành công!",
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
                    message = "Lỗi khi cập nhật loại phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpDelete("{maLoaiPhong}")]
        public async Task<IActionResult> DeleteLoaiPhong(int maLoaiPhong)
        {
            try
            {
                var result = await _loaiPhongRepository.DeleteLoaiPhongAsync(maLoaiPhong);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy loại phòng để xóa.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Xóa loại phòng thành công!",
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
                    message = "Lỗi khi xóa loại phòng: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }
}