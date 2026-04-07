using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class ReplyToReviewDto
    {
        [Required, MaxLength(1000)] public string AdminReply { get; set; } = string.Empty;
    }
}
