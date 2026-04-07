using HotelBookingAppWebApi.Models.DTOs.Auth;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Creates signed JWT tokens from a user payload.</summary>
    public interface ITokenService
    {
        /// <summary>Builds and signs a JWT token containing the user's id, name, role, and optional hotel id.</summary>
        string CreateToken(TokenPayloadDto payload);
    }
}
