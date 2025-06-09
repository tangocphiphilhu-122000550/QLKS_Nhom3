using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKS.Data;
using QLKS.Models;
using QLKS.Repository;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DichVuController : ControllerBase
    {
        private readonly IDichVuRepository _dichVuRepository;

        public DichVuController(IDichVuRepository dichVuRepository)
        {
            _dichVuRepository = dichVuRepository;
        }

        [HttpGet]
        [Authorize(Roles = "NhanVien,QuanLy")]
        public async Task<IActionResult> GetAllDichVu([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var dichVus = await _dichVuRepository.GetAllDichVu(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách dịch vụ thành công!",
                    data = dichVus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("search")]
        [Authorize(Roles = "NhanVien,QuanLy")]
        public async Task<IActionResult> GetDichVuByName([FromQuery] string tenDichVu)
        {
            try
            {
                if (string.IsNullOrEmpty(tenDichVu))
                {
                    return BadRequest(new
                    {
                        message = "Tên dịch vụ không được để trống.",
                        data = (object)null
                    });
                }

                var dichVus = await _dichVuRepository.GetDichVuByName(tenDichVu);
                if (dichVus == null || !dichVus.Any())
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy dịch vụ nào với tên này.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Tìm kiếm dịch vụ thành công!",
                    data = dichVus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi tìm dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> AddDichVu([FromBody] DichVuVM model)
        {
            try
            {
                var dichVuVM = await _dichVuRepository.AddDichVu(model);
                return Ok(new
                {
                    message = "Thêm dịch vụ thành công!",
                    data = dichVuVM
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
                    message = "Lỗi khi thêm dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPut("{tenDichVu}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> UpdateDichVu(string tenDichVu, [FromBody] DichVuVM model)
        {
            try
            {
                var result = await _dichVuRepository.UpdateDichVu(tenDichVu, model);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy dịch vụ để cập nhật.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Cập nhật dịch vụ thành công!",
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
                    message = "Lỗi khi cập nhật dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpDelete("{tenDichVu}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> DeleteDichVu(string tenDichVu)
        {
            try
            {
                var result = await _dichVuRepository.DeleteDichVu(tenDichVu);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy dịch vụ để xóa.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Xóa dịch vụ thành công!",
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi xóa dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }
}
