using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Room
{
    public class CreateRoomDto
    {
        [Required]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        public int Floor { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }
    }

    public class UpdateRoomDto
    {
        [Required]
        public Guid RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public Guid RoomTypeId { get; set; }
    }

    public class RoomListResponseDto
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public Guid RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
