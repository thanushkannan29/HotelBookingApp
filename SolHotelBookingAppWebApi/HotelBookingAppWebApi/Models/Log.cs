using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Log
    {
        [Key]
        public Guid LogId { get; set; }

        public string Message { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public string UserName { get; set; } = "Anonymous";
        public string Role { get; set; } = "Anonymous";
        public Guid? UserId { get; set; }

        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public string HttpMethod { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public User? User { get; set; }
    }
}
