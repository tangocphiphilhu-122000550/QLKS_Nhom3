using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using System.Threading.Tasks;

namespace QLKS.Helpers
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var path = context.Request.Path.Value?.ToLower();
                if (path.StartsWith("/swagger") ||
                    path.StartsWith("/api/auth/login") ||
                    path.StartsWith("/api/auth/tokens/refresh".ToLower()) ||  // Chuyển về lowercase để so sánh
                    path.StartsWith("/api/auth/logout"))
                {
                    await _next(context);
                    return;
                }

                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Thiếu token xác thực.");
                    return;
                }

                using var scope = context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataQlks112Nhom3Context>();

                var tokenRecord = await dbContext.Tokens
                    .FirstOrDefaultAsync(t => t.Token1 == token && t.IsRevoked);

                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

                if (tokenRecord == null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token không tồn tại trong cơ sở dữ liệu.");
                    return;
                }

                if (!tokenRecord.IsRevoked)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token đã bị thu hồi.");
                    return;
                }

                if (tokenRecord.TokenExpiry < currentTime)
                {
                    // CHỈ trả về 401, KHÔNG set IsRevoked = false
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token đã hết hạn.");
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Lỗi server: {ex.Message}");
            }
        } 
    }

    public static class TokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenValidationMiddleware>();
        }
    }
}