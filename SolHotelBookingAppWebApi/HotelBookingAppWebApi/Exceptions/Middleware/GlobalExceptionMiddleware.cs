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

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
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
            var message = ex is AppException ? ex.Message : "An unexpected error occurred";

            var user = context.User;

            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

            var userName = user?.Identity?.Name ?? "Anonymous";
            var role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "Anonymous";

            var controller = context.Request.RouteValues["controller"]?.ToString() ?? "";
            var action = context.Request.RouteValues["action"]?.ToString() ?? "";

            //  ILogger (IMPORTANT)
            _logger.LogError(ex,
                "Error: {Message}, User: {User}, Controller: {Controller}, Action: {Action}",
                message, userName, controller, action);

            //  Save log in DB
            try
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();

                var log = new Log
                {
                    LogId = Guid.NewGuid(),
                    Message = message,
                    ExceptionType = ex.GetType().Name,
                    StackTrace = ex.StackTrace ?? "",

                    StatusCode = statusCode,

                    UserId = userId,
                    UserName = userName,
                    Role = role,

                    Controller = controller,
                    Action = action,
                    HttpMethod = context.Request.Method,
                    RequestPath = context.Request.Path,

                    CreatedAt = DateTime.UtcNow
                };

                await db.Logs.AddAsync(log);
                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogCritical(logEx, "Error while saving log");
            }

            //  API Response
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
