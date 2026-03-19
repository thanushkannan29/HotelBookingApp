using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Models;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Exceptions.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var statusCode = ex is AppException appEx ? appEx.StatusCode : 500;
            var message = ex is AppException ? ex.Message : "An unexpected error occurred.";

            // Extract user info from JWT claims
            var user = context.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;
            var userName = user?.Identity?.Name ?? "Anonymous";
            var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "Anonymous";

            var controller = context.Request.RouteValues["controller"]?.ToString() ?? string.Empty;
            var action = context.Request.RouteValues["action"]?.ToString() ?? string.Empty;

            // ILogger — structured logging
            _logger.LogError(ex,
                "Exception | Status:{StatusCode} | User:{User} | Role:{Role} | {Controller}/{Action} | {Message}",
                statusCode, userName, role, controller, action, message);

            // Persist to DB log
            try
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();

                await db.Logs.AddAsync(new Log
                {
                    LogId = Guid.NewGuid(),
                    Message = message,
                    ExceptionType = ex.GetType().Name,
                    StackTrace = ex.StackTrace ?? string.Empty,
                    StatusCode = statusCode,
                    UserId = userId,
                    UserName = userName,
                    Role = role,
                    Controller = controller,
                    Action = action,
                    HttpMethod = context.Request.Method,
                    RequestPath = context.Request.Path,
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogCritical(logEx, "CRITICAL: Failed to persist exception log to database.");
            }

            // API Response
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                statusCode,
                message,
                traceId = context.TraceIdentifier
            });
        }
    }
}
