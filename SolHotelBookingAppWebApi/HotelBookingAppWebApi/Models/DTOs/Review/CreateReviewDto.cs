using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class CreateReviewDto
    {
        [Required]
        public Guid HotelId { get; set; }

        [Required]
        [Range(1, 5)]
        public decimal Rating { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}
