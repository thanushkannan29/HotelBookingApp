using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.QueryModels;

namespace HotelBookingAppWebApi.Interfaces.Repository
{
    public interface IHotelRepository : IRepository<Guid, Hotel>
    {
        Task<IEnumerable<TopHotelView>> GetTopHotelsAsync();

        Task<IEnumerable<Hotel>> SearchHotelsAsync(
            string city,
            int offset,
            int pageSize,
            DateTime checkIn,
            DateTime checkOut);

        Task<Hotel?> GetHotelDetailsAsync(Guid hotelId);

        Task<IEnumerable<RoomType>> GetRoomTypesAsync(Guid hotelId);

        Task<IEnumerable<RoomTypeInventory>> GetAvailabilityAsync(
            Guid hotelId,
            DateOnly checkIn,
            DateOnly checkOut);
    }
}
