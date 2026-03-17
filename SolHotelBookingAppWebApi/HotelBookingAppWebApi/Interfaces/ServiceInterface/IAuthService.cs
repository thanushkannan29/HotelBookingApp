using HotelBookingAppWebApi.Models.DTOs.Auth;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto);
        Task<AuthResponseDto> RegisterHotelAdminAsync(RegisterHotelAdminDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}
