using HotelBookingAppWebApi.Models.DTOs.Amenity;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IAmenityService
    {
        /// <summary>Returns all active amenities ordered by category then name</summary>
        Task<IEnumerable<AmenityResponseDto>> GetAllActiveAsync();

        /// <summary>Case-insensitive contains search on Name, returns up to 20 results</summary>
        Task<IEnumerable<AmenityResponseDto>> SearchAsync(string query);

        /// <summary>SuperAdmin only — creates a new amenity</summary>
        Task<AmenityResponseDto> CreateAmenityAsync(CreateAmenityDto dto);

        /// <summary>SuperAdmin only — updates an amenity</summary>
        Task<AmenityResponseDto> UpdateAmenityAsync(UpdateAmenityDto dto);

        /// <summary>SuperAdmin only — get all amenities paged (including inactive)</summary>
        Task<PagedAmenityResponseDto> GetAllAmenitiesPagedAsync(int page, int pageSize, string? search, string? category);

        /// <summary>SuperAdmin only — toggle IsActive flag</summary>
        Task<bool> ToggleAmenityStatusAsync(Guid amenityId);

        /// <summary>SuperAdmin only — hard delete if not in use</summary>
        Task<bool> DeleteAmenityAsync(Guid amenityId);
    }
}