namespace HotelBookingAppWebApi.Models.DTOs.Log
{
    public class LogResponseDto
    {
        public Guid LogId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
