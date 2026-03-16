using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces.Repository;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.QueryModels;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Repository
{
    public class HotelRepository : Repository<Guid, Hotel>, IHotelRepository
    {
        public HotelRepository(HotelBookingContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TopHotelView>> GetTopHotelsAsync()
        {
            return await _context.TopHotelViews
                .FromSqlRaw("EXEC proc_GetTopHotels")
                .ToListAsync();
        }

        public async Task<IEnumerable<Hotel>> SearchHotelsAsync(
            string city,
            int offset,
            int pageSize,
            DateTime checkIn,
            DateTime checkOut)
        {
            return await _context.Hotels
                .FromSqlRaw("EXEC proc_SearchHotels {0},{1},{2},{3},{4}",
                city, offset, pageSize, checkIn, checkOut)
                .ToListAsync();
        }

        public async Task<Hotel?> GetHotelDetailsAsync(Guid hotelId)
        {
            return await _context.Hotels
                .Include(h => h.RoomTypes)
                .Include(h => h.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(h => h.HotelId == hotelId);
        }

        public async Task<IEnumerable<RoomType>> GetRoomTypesAsync(Guid hotelId)
        {
            return await _context.RoomTypes
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<RoomTypeInventory>> GetAvailabilityAsync(
            Guid hotelId,
            DateOnly checkIn,
            DateOnly checkOut)
        {
            return await _context.RoomTypeInventories
                .Include(i => i.RoomType)
                .Where(i =>
                    i.RoomType!.HotelId == hotelId &&
                    i.Date >= checkIn &&
                    i.Date <= checkOut)
                .ToListAsync();
        }
    }
}
