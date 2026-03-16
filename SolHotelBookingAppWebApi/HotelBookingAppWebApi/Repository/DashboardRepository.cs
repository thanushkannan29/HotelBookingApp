using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Repository
{
    public class DashboardRepository
    {
        private readonly HotelBookingContext _context;

        public DashboardRepository(HotelBookingContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalHotelsAsync()
        {
            return await _context.Hotels.CountAsync();
        }

        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetTotalReservationsAsync()
        {
            return await _context.Reservations.CountAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == PaymentStatus.Success)
                .SumAsync(t => t.Amount);
        }

        public async Task<int> GetTotalReviewsAsync()
        {
            return await _context.Reviews.CountAsync();
        }

        public async Task<int> GetHotelRoomsAsync(Guid hotelId)
        {
            return await _context.Rooms
                .Where(r => r.HotelId == hotelId)
                .CountAsync();
        }

        public async Task<int> GetHotelReservationsAsync(Guid hotelId)
        {
            return await _context.Reservations
                .Where(r => r.HotelId == hotelId)
                .CountAsync();
        }

        public async Task<int> GetActiveHotelReservationsAsync(Guid hotelId)
        {
            return await _context.Reservations
                .Where(r => r.HotelId == hotelId &&
                       r.Status == ReservationStatus.Confirmed)
                .CountAsync();
        }

        public async Task<decimal> GetHotelRevenueAsync(Guid hotelId)
        {
            return await _context.Transactions
                .Where(t => t.Reservation!.HotelId == hotelId &&
                       t.Status == PaymentStatus.Success)
                .SumAsync(t => t.Amount);
        }

        public async Task<int> GetHotelReviewsAsync(Guid hotelId)
        {
            return await _context.Reviews
                .Where(r => r.HotelId == hotelId)
                .CountAsync();
        }

        public async Task<decimal> GetHotelAverageRatingAsync(Guid hotelId)
        {
            var reviews = _context.Reviews.Where(r => r.HotelId == hotelId);

            if (!await reviews.AnyAsync())
                return 0;

            return await reviews.AverageAsync(r => r.Rating);
        }

        public async Task<int> GetGuestReservationsAsync(Guid userId)
        {
            return await _context.Reservations
                .Where(r => r.UserId == userId)
                .CountAsync();
        }

        public async Task<int> GetGuestActiveReservationsAsync(Guid userId)
        {
            return await _context.Reservations
                .Where(r => r.UserId == userId &&
                       r.Status == ReservationStatus.Confirmed)
                .CountAsync();
        }

        public async Task<int> GetGuestCompletedReservationsAsync(Guid userId)
        {
            return await _context.Reservations
                .Where(r => r.UserId == userId &&
                       r.Status == ReservationStatus.Completed)
                .CountAsync();
        }

        public async Task<int> GetGuestCancelledReservationsAsync(Guid userId)
        {
            return await _context.Reservations
                .Where(r => r.UserId == userId &&
                       r.Status == ReservationStatus.Cancelled)
                .CountAsync();
        }

        public async Task<decimal> GetGuestTotalSpentAsync(Guid userId)
        {
            return await _context.Transactions
                .Where(t => t.Reservation!.UserId == userId &&
                       t.Status == PaymentStatus.Success)
                .SumAsync(t => t.Amount);
        }
    }
}
