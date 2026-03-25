using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.City
{
    public class CityDto
    {
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCityDto
    {
        [Required, MaxLength(100)]
        public string CityName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string StateName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string PinCode { get; set; } = string.Empty;
    }

    public class UpdateCityDto
    {
        [Required, MaxLength(100)]
        public string CityName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string StateName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string PinCode { get; set; } = string.Empty;
    }

    public class PagedCityResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<CityDto> Cities { get; set; } = new List<CityDto>();
    }
}
