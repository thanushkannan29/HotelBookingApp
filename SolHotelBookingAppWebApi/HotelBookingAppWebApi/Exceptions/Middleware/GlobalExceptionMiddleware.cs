using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Models;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Exceptions.Middleware
{
    /// <summary>
    /// Global exception handler middleware.
    /// Catches all unhandled exceptions, logs them to ILogger and the DB Logs table,
    /// and returns a consistent JSON error envelope to the client.
    /// Must be registered BEFORE UseAuthentication so it catches auth failures too.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // ── PIPELINE ENTRY ────────────────────────────────────────────────────

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

        // ── EXCEPTION HANDLING ────────────────────────────────────────────────

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, message) = ResolveStatusAndMessage(ex);
            var requestInfo = ExtractRequestInfo(context);

            LogToStructuredLogger(ex, statusCode, message, requestInfo);
            await PersistLogToDatabaseAsync(context, ex, statusCode, message, requestInfo);
            await WriteJsonResponseAsync(context, statusCode, message);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private static (int statusCode, string message) ResolveStatusAndMessage(Exception ex)
        {
            var statusCode = ex is AppException appEx ? appEx.StatusCode : 500;
            var message = ex is AppException ? ex.Message : "An unexpected error occurred.";
            return (statusCode, message);
        }

        private static RequestInfo ExtractRequestInfo(HttpContext context)
        {
            var user = context.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new RequestInfo
            {
                UserId = Guid.TryParse(userIdClaim, out var uid) ? uid : null,
                UserName = user?.Identity?.Name ?? "Anonymous",
                Role = user?.FindFirst(ClaimTypes.Role)?.Value ?? "Anonymous",
                Controller = context.Request.RouteValues["controller"]?.ToString() ?? string.Empty,
                Action = context.Request.RouteValues["action"]?.ToString() ?? string.Empty,
                HttpMethod = context.Request.Method,
                RequestPath = context.Request.Path
            };
        }

        private void LogToStructuredLogger(
            Exception ex, int statusCode, string message, RequestInfo info)
        {
            _logger.LogError(ex,
                "Exception | Status:{StatusCode} | User:{User} | Role:{Role} | {Controller}/{Action} | {Message}",
                statusCode, info.UserName, info.Role, info.Controller, info.Action, message);
        }

        private async Task PersistLogToDatabaseAsync(
            HttpContext context, Exception ex,
            int statusCode, string message, RequestInfo info)
        {
            try
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();
                await db.Logs.AddAsync(BuildLogEntry(ex, statusCode, message, info));
                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogCritical(logEx, "CRITICAL: Failed to persist exception log to database.");
            }
        }

        private static async Task WriteJsonResponseAsync(
            HttpContext context, int statusCode, string message)
        {
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

        private static Log BuildLogEntry(
            Exception ex, int statusCode, string message, RequestInfo info) => new()
        {
            LogId = Guid.NewGuid(),
            Message = message,
            ExceptionType = ex.GetType().Name,
            StackTrace = ex.StackTrace ?? string.Empty,
            StatusCode = statusCode,
            UserId = info.UserId,
            UserName = info.UserName,
            Role = info.Role,
            Controller = info.Controller,
            Action = info.Action,
            HttpMethod = info.HttpMethod,
            RequestPath = info.RequestPath,
            CreatedAt = DateTime.UtcNow
        };

        // ── INNER VALUE TYPE ──────────────────────────────────────────────────

        private sealed record RequestInfo
        {
            public Guid? UserId { get; init; }
            public string UserName { get; init; } = string.Empty;
            public string Role { get; init; } = string.Empty;
            public string Controller { get; init; } = string.Empty;
            public string Action { get; init; } = string.Empty;
            public string HttpMethod { get; init; } = string.Empty;
            public string RequestPath { get; init; } = string.Empty;
        }
    }
}
