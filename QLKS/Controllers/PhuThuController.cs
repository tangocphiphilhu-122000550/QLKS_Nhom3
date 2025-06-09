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
                var result = await _phuThuRepository.GetAllPhuThu(pageNumber, pageSize);
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách phụ thu thành công!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy danh sách phụ thu: " + ex.Message
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> AddPhuThu([FromBody] PhuThuVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _phuThuRepository.AddPhuThu(model);
                return Ok(new
                {
                    success = true,
                    message = "Thêm phụ thu thành công!",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi thêm phụ thu: " + ex.Message
                });
            }
        }

        [HttpPut("{maPhuThu}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> UpdatePhuThu(int maPhuThu, [FromBody] PhuThuVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _phuThuRepository.UpdatePhuThu(maPhuThu, model);
                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy phụ thu để cập nhật."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật phụ thu thành công!"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi cập nhật phụ thu: " + ex.Message
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
                        success = false,
                        message = "Không tìm thấy phụ thu để xóa."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Xóa phụ thu thành công!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi xóa phụ thu: " + ex.Message
                });
            }
        }
    }
}
