using HotelBookingAppWebApi.Models.DTOs.City;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ICityService
    {
        Task<IEnumerable<CityDto>> SearchCitiesAsync(string? search);
        Task<IEnumerable<CityDto>> GetAllActiveCitiesAsync();
        Task<PagedCityResponseDto> GetAllCitiesPagedAsync(int page, int pageSize, string? search);
        Task<CityDto> AddCityAsync(CreateCityDto dto);
        Task<CityDto> UpdateCityAsync(Guid cityId, UpdateCityDto dto);
        Task<bool> ToggleCityStatusAsync(Guid cityId);
        Task<bool> DeleteCityAsync(Guid cityId);
    }
}
