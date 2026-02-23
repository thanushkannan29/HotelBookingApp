using HotelBookingAppWebApi.Models.DTOs;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IUserService
    {
        public Task<CheckUserResponseDto> CheckUser(CheckUserRequestDto request);
    }
}
