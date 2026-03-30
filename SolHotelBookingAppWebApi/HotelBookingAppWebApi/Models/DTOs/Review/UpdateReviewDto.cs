using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class UpdateReviewDto
    {
        [Range(1, 5)] public decimal Rating { get; set; }
        [MaxLength(1000)] public string? Comment { get; set; }
        public string? ImageUrl { get; set; }
    }
}
