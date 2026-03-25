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

        // Legacy string field (kept for backward compat)
        public string Amenities { get; set; } = string.Empty;

        /// <summary>New: list of amenity IDs from the Amenities master table</summary>
        public List<Guid>? AmenityIds { get; set; }

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

        // Legacy string field (kept for backward compat)
        public string Amenities { get; set; } = string.Empty;

        /// <summary>New: list of amenity IDs from the Amenities master table</summary>
        public List<Guid>? AmenityIds { get; set; }

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
        public List<AmenityItemDto> AmenityList { get; set; } = new();
        public bool IsActive { get; set; }
        public int RoomCount { get; set; }

        /// <summary>Optional room type photo URL</summary>
        public string? ImageUrl { get; set; }
    }

    public class AmenityItemDto
    {
        public Guid AmenityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? IconName { get; set; }
    }

    public class PagedRoomTypeResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<RoomTypeListDto> RoomTypes { get; set; } = new List<RoomTypeListDto>();
    }

    public class RoomTypeRateDto
    {
        public Guid RoomTypeRateId { get; set; }
        public Guid RoomTypeId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Rate { get; set; }
    }
}