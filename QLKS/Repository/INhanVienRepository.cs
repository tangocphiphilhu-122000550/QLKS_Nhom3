using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QLKS.Data;
using QLKS.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QLKS.Repository
{
    public interface INhanVienRepository
    {
        Task<NhanVien> Register(NhanVien nhanVien);
        Task<(NhanVien NhanVien, string Token, string RefreshToken)> Login(string email, string matKhau);
        Task<NhanVien> GetNhanVienByEmail(string email);
        Task<bool> UpdatePassword(string email, byte[] newPassword);
        Task<bool> ForgotPassword(string email);
        Task<(string NewToken, string NewRefreshToken)> RefreshToken(string token, string refreshToken);
        Task<bool> RevokeToken(string token);
    }

    public class NhanVienRepository : INhanVienRepository
    {
        private readonly DataQlks112Nhom3Context _context;
        private readonly EmailHelper _emailHelper;
        private readonly IConfiguration _configuration;

        public NhanVienRepository(DataQlks112Nhom3Context context, EmailHelper emailHelper, IConfiguration configuration)
        {
            _context = context;
            _emailHelper = emailHelper;
            _configuration = configuration;
        }

        public async Task<NhanVien> Register(NhanVien nhanVien)
        {
            var existingUser = await GetNhanVienByEmail(nhanVien.Email);
            if (existingUser == null)
                throw new Exception("Email chưa được thêm vào hệ thống. Vui lòng dùng API AddAccount trước.");

            if (!existingUser.IsActive)
                throw new Exception("Tài khoản đã bị vô hiệu hóa, không thể đăng ký.");

            if (existingUser.MatKhau != null && existingUser.MatKhau.Length > 0)
                throw new Exception("Tài khoản đã được đăng ký trước đó.");

            try
            {
                existingUser.MatKhau = nhanVien.MatKhau; 
                await _context.SaveChangesAsync();
                return existingUser;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Lỗi khi lưu dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        public async Task<(NhanVien NhanVien, string Token, string RefreshToken)> Login(string email, string matKhau)
        {
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.MaVaiTroNavigation)
                .FirstOrDefaultAsync(nv => nv.Email == email && nv.IsActive);

            if (nhanVien == null || !BCrypt.Net.BCrypt.Verify(matKhau, Encoding.UTF8.GetString(nhanVien.MatKhau)))
            {
                return (null, null, null);
            }

            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            var token = GenerateJwtToken(nhanVien);
            var refreshToken = GenerateRefreshToken();

            var tokenEntity = new Token
            {
                MaNv = nhanVien.MaNv,
                Token1 = token,
                RefreshToken = refreshToken,
                TokenExpiry = currentTime.AddHours(1),
                RefreshTokenExpiry = currentTime.AddDays(7), 
                CreatedAt = currentTime, 
                IsRevoked = true 
            };

            _context.Tokens.Add(tokenEntity);
            await _context.SaveChangesAsync();

            return (nhanVien, token, refreshToken);
        }

        public async Task<NhanVien> GetNhanVienByEmail(string email)
        {
            return await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == email);
        }

        public async Task<bool> UpdatePassword(string email, byte[] newPassword)
        {
            var nhanVien = await GetNhanVienByEmail(email);
            if (nhanVien == null || !nhanVien.IsActive)
            {
                return false;
            }

            nhanVien.MatKhau = newPassword;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ForgotPassword(string email)
        {
            var nhanVien = await GetNhanVienByEmail(email);
            if (nhanVien == null || !nhanVien.IsActive)
            {
                return false;
            }

            string newPassword = GenerateRandomPassword(8);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(hashedPassword);

            var success = await UpdatePassword(email, passwordBytes);
            if (!success)
            {
                return false;
            }

            string subject = "Yêu Cầu Đặt Lại Mật Khẩu - Khách Sạn Nhóm 3";
            string body = $@"
                <!DOCTYPE html>
                <html>
                <body>
                    <h3>Xin chào {nhanVien.HoTen},</h3>
                    <p>Mật khẩu mới của bạn là: <strong>{newPassword}</strong></p>
                    <p>Vui lòng đổi mật khẩu sau khi đăng nhập.</p>
                </body>
                </html>";

            await _emailHelper.SendEmailAsync(email, subject, body, isHtml: true);
            return true;
        }

        public async Task<(string NewToken, string NewRefreshToken)> RefreshToken(string token, string refreshToken)
        {
            var tokenEntity = await _context.Tokens
                .FirstOrDefaultAsync(t => t.Token1 == token && t.RefreshToken == refreshToken && t.IsRevoked);

            if (tokenEntity == null)
            {
                throw new Exception("Token hoặc refresh token không hợp lệ.");
            }

            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            if (tokenEntity.RefreshTokenExpiry < currentTime)
            {
                // Đặt IsRevoked thành false khi refresh token hết hạn
                tokenEntity.IsRevoked = false;
                await _context.SaveChangesAsync();
                throw new Exception("Refresh token đã hết hạn. Vui lòng đăng nhập lại.");
            }

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.MaVaiTroNavigation)
                .FirstOrDefaultAsync(nv => nv.MaNv == tokenEntity.MaNv);

            if (nhanVien == null || !nhanVien.IsActive)
            {
                throw new Exception("Nhân viên không tồn tại hoặc tài khoản đã bị vô hiệu hóa.");
            }

            // Vô hiệu hóa token cũ bằng cách đặt IsRevoked thành false
            tokenEntity.IsRevoked = false;
            await _context.SaveChangesAsync();

            // Tạo token mới và refresh token mới
            var newToken = GenerateJwtToken(nhanVien);
            var newRefreshToken = GenerateRefreshToken();

            // Tạo bản ghi mới trong bảng Tokens
            var newTokenEntity = new Token
            {
                MaNv = nhanVien.MaNv,
                Token1 = newToken,
                RefreshToken = newRefreshToken,
                TokenExpiry = currentTime.AddHours(1),
                RefreshTokenExpiry = currentTime.AddDays(7),
                CreatedAt = currentTime,
                IsRevoked = true
            };

            _context.Tokens.Add(newTokenEntity);
            await _context.SaveChangesAsync();

            return (newToken, newRefreshToken);
        }

        public async Task<bool> RevokeToken(string token)
        {
            var tokenEntity = await _context.Tokens
                .FirstOrDefaultAsync(t => t.Token1 == token);

            if (tokenEntity == null)
            {
                return false;
            }

            // Đặt IsRevoked thành false để vô hiệu hóa token
            tokenEntity.IsRevoked = false;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(NhanVien nhanVien)
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, nhanVien.MaNv.ToString()),
                new Claim(ClaimTypes.Email, nhanVien.Email),
                new Claim(ClaimTypes.Role, nhanVien.MaVaiTroNavigation?.TenVaiTro ?? "NhanVien"),
                new Claim("MaNv", nhanVien.MaNv.ToString()),
                new Claim("HoTen", nhanVien.HoTen ?? "")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: currentTime.AddHours(1), // Đồng bộ với TokenExpiry
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private string GenerateRandomPassword(int length)
        {
            const string allChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            Random random = new Random();
            return new string(Enumerable.Repeat(allChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}