using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Models.QueryModels
{
    [Keyless]
    public class RoomListQueryModel
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public bool IsActive { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
    }
}
