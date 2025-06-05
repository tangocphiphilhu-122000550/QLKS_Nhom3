using Microsoft.AspNetCore.Mvc;
using QLKS.Models;
using QLKS.Repository;
using QLKS.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace QLKS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountRepository _repository;

        public AccountController(IAccountRepository repository)
        {
            _repository = repository;
        }

        [Authorize(Roles = "QuanLy")]
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _repository.GetAllAccounts(pageNumber, pageSize);
                return Ok(new
                {
                    message = "Lấy danh sách tài khoản thành công!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpGet("by-name")]
        public async Task<IActionResult> GetByNameNhanVien([FromQuery] string hoTen)
        {
            try
            {
                if (string.IsNullOrEmpty(hoTen))
                {
                    return BadRequest(new
                    {
                        message = "Họ tên không được để trống.",
                        data = (object)null
                    });
                }

                var accounts = await _repository.GetByNameNhanVien(hoTen);
                var result = accounts.Select(nv => new Account
                {
                    HoTen = nv.HoTen,
                    MaVaiTro = nv.MaVaiTro,
                    SoDienThoai = nv.SoDienThoai,
                    Email = nv.Email,
                    GioiTinh = nv.GioiTinh,
                    DiaChi = nv.DiaChi,
                    NgaySinh = nv.NgaySinh
                }).ToList();
                return Ok(new
                {
                    message = "Tìm kiếm tài khoản thành công!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi tìm kiếm: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpPost]
        public async Task<IActionResult> AddAccount([FromBody] Account model)
        {
            try
            {
                var nhanVien = new NhanVien
                {
                    HoTen = model.HoTen,
                    Email = model.Email,
                    SoDienThoai = model.SoDienThoai,
                    MaVaiTro = model.MaVaiTro,
                    GioiTinh = model.GioiTinh,
                    DiaChi = model.DiaChi,
                    NgaySinh = model.NgaySinh
                };

                var addedAccount = await _repository.AddAccount(nhanVien);
                return Ok(new
                {
                    message = "Thêm tài khoản thành công!",
                    data = new { Email = addedAccount.Email }
                });
            }
            catch (DbUpdateException ex)
            {
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new
                {
                    message = "Lỗi khi thêm tài khoản: " + ex.Message,
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Lỗi khi thêm tài khoản: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpPut("{email}")]
        public async Task<IActionResult> UpdateAccount(string email, [FromBody] UpdateAccountDTO model)
        {
            try
            {
                var success = await _repository.UpdateAccount(email, model);
                if (!success)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy tài khoản để cập nhật.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Cập nhật tài khoản thành công!",
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Lỗi khi cập nhật tài khoản: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [Authorize(Roles = "QuanLy")]
        [HttpDelete("{email}")]
        public async Task<IActionResult> DeleteAccount(string email)
        {
            try
            {
                var success = await _repository.DeleteAccount(email);
                if (!success)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy tài khoản hoặc tài khoản đã bị vô hiệu hóa.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Vô hiệu hóa tài khoản thành công!",
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi vô hiệu hóa tài khoản: " + ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPut("restore/{email}")]
        [Authorize(Roles = "QuanLy")]
        public async Task<IActionResult> RestoreAccount(string email)
        {
            try
            {
                var success = await _repository.RestoreAccount(email);
                if (!success)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy tài khoản hoặc tài khoản đã hoạt động.",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    message = "Khôi phục tài khoản thành công!",
                    data = (object)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi khôi phục tài khoản: " + ex.Message,
                    data = (object)null
                });
            }
        }
    }
}