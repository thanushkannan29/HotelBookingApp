using HotelBookingAppWebApi.Models.DTOs.Auth;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(TokenPayloadDto payload);
    }
}
