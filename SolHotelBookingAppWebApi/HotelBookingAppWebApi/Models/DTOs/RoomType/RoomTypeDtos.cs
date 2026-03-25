using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class CreateRoomTypeDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public int MaxOccupancy { get; set; }

        public string Amenities { get; set; } = string.Empty;

        /// <summary>Optional room type photo URL</summary>
        public string? ImageUrl { get; set; }
    }

    public class UpdateRoomTypeDto
    {
        [Required]
        public Guid RoomTypeId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxOccupancy { get; set; }
        public string Amenities { get; set; } = string.Empty;

        /// <summary>Optional room type photo URL</summary>
        public string? ImageUrl { get; set; }
    }

    public class CreateRoomTypeRateDto
    {
        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [Required]
        public decimal Rate { get; set; }
    }

    public class UpdateRoomTypeRateDto
    {
        [Required]
        public Guid RoomTypeRateId { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Rate { get; set; }
    }

    public class GetRateByDateRequestDto
    {
        public Guid RoomTypeId { get; set; }
        public DateOnly Date { get; set; }
    }

    public class RoomTypeListDto
    {
        public Guid RoomTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxOccupancy { get; set; }
        public string Amenities { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int RoomCount { get; set; }

        /// <summary>Optional room type photo URL</summary>
        public string? ImageUrl { get; set; }
    }

    public class PagedRoomTypeResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<RoomTypeListDto> RoomTypes { get; set; } = new List<RoomTypeListDto>();
    }
}