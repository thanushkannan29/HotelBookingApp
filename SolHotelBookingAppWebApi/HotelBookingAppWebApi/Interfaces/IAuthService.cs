using HotelBookingAppWebApi.Models.DTOs.Auth;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Authentication operations — registration and login for all roles.</summary>
    public interface IAuthService
    {
        /// <summary>Registers a new guest account and returns a JWT token.</summary>
        Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto);

        /// <summary>Registers a new hotel admin together with their hotel and returns a JWT token.</summary>
        Task<AuthResponseDto> RegisterHotelAdminAsync(RegisterHotelAdminDto dto);

        /// <summary>Validates credentials for any role and returns a JWT token.</summary>
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}
