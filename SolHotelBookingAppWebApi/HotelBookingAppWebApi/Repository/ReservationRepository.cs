using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces.Repository;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;
namespace HotelBookingAppWebApi.Repository
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly HotelBookingContext _context;

        public ReservationRepository(HotelBookingContext context)
        {
            _context = context;
        }

        public async Task<RoomType?> GetRoomTypeAsync(Guid roomTypeId, Guid hotelId)
        {
            return await _context.RoomTypes
                .FirstOrDefaultAsync(r =>
                    r.RoomTypeId == roomTypeId &&
                    r.HotelId == hotelId &&
                    r.IsActive);
        }

        public async Task<int> GetPhysicalRoomsAsync(Guid roomTypeId, Guid hotelId)
        {
            return await _context.Rooms
                .CountAsync(r =>
                    r.RoomTypeId == roomTypeId &&
                    r.HotelId == hotelId &&
                    r.IsActive);
        }

        public async Task<List<RoomTypeInventory>> GetInventoriesAsync(Guid roomTypeId, List<DateOnly> dates)
        {
            return await _context.RoomTypeInventories
                .Where(i =>
                    i.RoomTypeId == roomTypeId &&
                    dates.Contains(i.Date))
                .ToListAsync();
        }

        public async Task<List<RoomTypeRate>> GetRatesAsync(Guid roomTypeId, DateOnly checkIn, DateOnly checkOut)
        {
            return await _context.RoomTypeRates
                .Where(r =>
                    r.RoomTypeId == roomTypeId &&
                    r.StartDate <= checkOut &&
                    r.EndDate >= checkIn)
                .ToListAsync();
        }

        public async Task AddReservationAsync(Reservation reservation)
        {
            await _context.Reservations.AddAsync(reservation);
        }

        public async Task AddReservationRoomAsync(ReservationRoom room)
        {
            await _context.ReservationRooms.AddAsync(room);
        }

        public async Task<Reservation?> GetReservationByCodeAsync(string code, Guid userId)
        {
            return await _context.Reservations
                .Include(r => r.ReservationRooms)
                .FirstOrDefaultAsync(r =>
                    r.ReservationCode == code &&
                    r.UserId == userId);
        }

        public async Task<List<Reservation>> GetUserReservationsAsync(Guid userId)
        {
            return await _context.Reservations
                .Include(r => r.ReservationRooms)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<Reservation?> GetReservationForCancelAsync(string code, Guid userId)
        {
            return await _context.Reservations
                .Include(r => r.ReservationRooms)
                .FirstOrDefaultAsync(r =>
                    r.ReservationCode == code &&
                    r.UserId == userId);
        }

        public async Task<Reservation?> GetReservationForAdminAsync(string code)
        {
            return await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationCode == code);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<List<Room>> GetAvailableRoomsAsync(Guid roomTypeId, Guid hotelId)
        {
            return await _context.Rooms
                .Where(r =>
                    r.RoomTypeId == roomTypeId &&
                    r.HotelId == hotelId &&
                    r.IsActive)
                .ToListAsync();
        }


    }
}
