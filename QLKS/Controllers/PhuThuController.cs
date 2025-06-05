using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKS.Models;
using QLKS.Repository;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PhuThuController : ControllerBase
    {
        private readonly IPhuThuRepository _phuThuRepository;

        public PhuThuController(IPhuThuRepository phuThuRepository)
        {
            _phuThuRepository = phuThuRepository;
        }

        [HttpGet]
        [Authorize(Roles = "NhanVien,QuanLy")]
        public async Task<IActionResult> GetAllPhuThu([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var phuThus = await _phuThuRepository.GetAllPhuThu(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách phụ thu thành công!",
                    data = phuThus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách phụ thu: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("loai-phong/{maLoaiPhong}")]
        [Authorize(Roles = "NhanVien")]
        public async Task<IActionResult> GetPhuThuByLoaiPhong(int maLoaiPhong)
        {
            try
            {
                var phuThus = await _phuThuRepository.GetPhuThuByLoaiPhong(maLoaiPhong);
                if (phuThus == null || !phuThus.Any())
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phụ thu cho loại phòng này.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Tìm kiếm phụ thu thành công!",
                    data = phuThus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi tìm phụ thu: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> AddPhuThu([FromBody] PhuThuVM model)
        {
            try
            {
                var phuThuVM = await _phuThuRepository.AddPhuThu(model);
                return Ok(new
                {
                    message = "Thêm phụ thu thành công!",
                    data = phuThuVM
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
                    message = "Lỗi khi thêm phụ thu: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPut("{maPhuThu}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> UpdatePhuThu(int maPhuThu, [FromBody] PhuThuVM model)
        {
            try
            {
                var result = await _phuThuRepository.UpdatePhuThu(maPhuThu, model);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phụ thu để cập nhật.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Cập nhật phụ thu thành công!",
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
                    message = "Lỗi khi cập nhật phụ thu: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpDelete("{maPhuThu}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> DeletePhuThu(int maPhuThu)
        {
            try
            {
                var result = await _phuThuRepository.DeletePhuThu(maPhuThu);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy phụ thu để xóa.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Xóa phụ thu thành công!",
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi xóa phụ thu: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }
} 