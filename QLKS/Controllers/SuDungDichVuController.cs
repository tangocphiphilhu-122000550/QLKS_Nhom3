using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKS.Models;
using QLKS.Repository;
using System.Threading.Tasks;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SuDungDichVuController : ControllerBase
    {
        private readonly ISuDungDichVuRepository _suDungDichVuRepository;

        public SuDungDichVuController(ISuDungDichVuRepository suDungDichVuRepository)
        {
            _suDungDichVuRepository = suDungDichVuRepository;
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpGet]
        public async Task<IActionResult> GetAllSuDungDichVu([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var suDungDichVus = await _suDungDichVuRepository.GetAllSuDungDichVu(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách sử dụng dịch vụ thành công!",
                    data = suDungDichVus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách sử dụng dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPost]
        public async Task<IActionResult> AddSuDungDichVu([FromBody] CreateSuDungDichVuVM model)
        {
            try
            {
                var result = await _suDungDichVuRepository.AddSuDungDichVu(model);
                if (!result)
                {
                    return BadRequest(new
                    {
                        message = "Không thể tạo sử dụng dịch vụ.",
                        data = (object)null
                    });
                }
                return Ok(new
                {
                    message = "Tạo sử dụng dịch vụ thành công!",
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
                    message = "Lỗi khi thêm sử dụng dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "NhanVien,QuanLy")]
        [HttpPut("{maSuDung}")]
        public async Task<IActionResult> UpdateSuDungDichVu(int maSuDung, [FromBody] SuDungDichVuVM model)
        {
            try
            {
                var result = await _suDungDichVuRepository.UpdateSuDungDichVu(maSuDung, model);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy bản ghi sử dụng dịch vụ để cập nhật.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Cập nhật sử dụng dịch vụ thành công!",
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
                    message = "Lỗi khi cập nhật sử dụng dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy,NhanVien")]
        [HttpDelete("{maSuDung}")]
        public async Task<IActionResult> DeleteSuDungDichVu(int maSuDung)
        {
            try
            {
                var result = await _suDungDichVuRepository.DeleteSuDungDichVu(maSuDung);
                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy bản ghi sử dụng dịch vụ để xóa.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Xóa sử dụng dịch vụ thành công!",
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi xóa sử dụng dịch vụ: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }
}
