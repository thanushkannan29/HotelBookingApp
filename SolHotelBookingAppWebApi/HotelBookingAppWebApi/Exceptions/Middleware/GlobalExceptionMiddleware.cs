using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Models;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Exceptions.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, HotelBookingContext db)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var userIdClaim = context.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdClaim, out Guid userId))
                {
                    var user = await db.Users.FindAsync(userId);

                    if (user != null)
                    {
                        var log = new Log
                        {
                            LogId = Guid.NewGuid(),
                            Message = ex.Message,
                            ErrorNumber = ex.HResult.ToString(),
                            Role = user.Role.ToString(),
                            UserName = user.Email,
                            UserId = userId,
                            CreatedAt = DateTime.UtcNow
                        };

                        await db.Logs.AddAsync(log);
                        await db.SaveChangesAsync();
                    }
                }

                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(
                    new { message = "An unexpected error occurred." });
            }
        }
    }
}
