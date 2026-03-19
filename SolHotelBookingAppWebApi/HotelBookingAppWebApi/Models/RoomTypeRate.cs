using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class RoomTypeRate
    {
        [Key]
        public Guid RoomTypeRateId { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [Required]
        public decimal Rate { get; set; }

        public RoomType? RoomType { get; set; }
    }
}
