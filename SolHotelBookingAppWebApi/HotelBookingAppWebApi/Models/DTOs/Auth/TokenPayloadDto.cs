namespace HotelBookingAppWebApi.Models.DTOs.Auth
{
    /// <summary>Internal payload used to build the JWT claims.</summary>
    public class TokenPayloadDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? HotelId { get; set; }
    }
}
