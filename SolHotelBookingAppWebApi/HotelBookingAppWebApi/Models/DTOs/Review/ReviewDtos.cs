using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class CreateReviewDto
    {
        [Required]
        public Guid HotelId { get; set; }

        /// <summary>One review per completed reservation � required</summary>
        [Required]
        public Guid ReservationId { get; set; }

        [Required, Range(1, 5)]
        public decimal Rating { get; set; }

        [Required, MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }

    public class UpdateReviewDto
    {
        [Range(1, 5)]
        public decimal Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class ReviewResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid HotelId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        /// <summary>Which reservation this review is for</summary>
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;

        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? UserProfileImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AdminReply { get; set; }

        /// <summary>Always 100 pts per review</summary>
        public int ContributionPoints { get; set; } = 100;
    }

    public class ReplyToReviewDto
    {
        [Required, MaxLength(1000)]
        public string AdminReply { get; set; } = string.Empty;
    }

    public class MyReviewsResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;

        /// <summary>Which reservation this review is for</summary>
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;

        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }

        /// <summary>Always 100 pts per review</summary>
        public int ContributionPoints { get; set; } = 100;
    }

    public class PagedReviewResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<ReviewResponseDto> Reviews { get; set; } = new List<ReviewResponseDto>();
    }

    public class PagedMyReviewsResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<MyReviewsResponseDto> Reviews { get; set; } = new List<MyReviewsResponseDto>();
    }

    public class GetHotelReviewsRequestDto
    {
        public Guid HotelId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}