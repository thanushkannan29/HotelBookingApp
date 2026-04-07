namespace HotelBookingAppWebApi.Models.DTOs.Auth
{
    /// <summary>Returned on successful login or registration — contains the JWT token.</summary>
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }
}
