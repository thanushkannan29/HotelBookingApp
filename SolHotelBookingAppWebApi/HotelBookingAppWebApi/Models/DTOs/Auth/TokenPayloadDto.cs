namespace HotelBookingAppWebApi.Models.DTOs.Auth
{
    public class TokenPayloadDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? HotelId { get; set; }
    }
}
