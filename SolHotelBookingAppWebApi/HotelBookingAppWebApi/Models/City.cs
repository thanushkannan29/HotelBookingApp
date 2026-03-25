using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class City
    {
        [Key]
        public Guid CityId { get; set; }

        [Required, MaxLength(100)]
        public string CityName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string StateName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string PinCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
    }
}
