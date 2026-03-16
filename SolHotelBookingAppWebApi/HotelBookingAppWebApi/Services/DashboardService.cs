using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Dashboard;
using HotelBookingAppWebApi.Repository;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly DashboardRepository _repo;
        private readonly HotelBookingContext _context;

        public DashboardService(DashboardRepository repo, HotelBookingContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.HotelId == null)
                throw new NotFoundException("Admin hotel not found");

            var hotelId = user.HotelId.Value;

            return new AdminDashboardDto
            {
                HotelId = hotelId,
                TotalRooms = await _repo.GetHotelRoomsAsync(hotelId),
                TotalReservations = await _repo.GetHotelReservationsAsync(hotelId),
                ActiveReservations = await _repo.GetActiveHotelReservationsAsync(hotelId),
                TotalRevenue = await _repo.GetHotelRevenueAsync(hotelId),
                TotalReviews = await _repo.GetHotelReviewsAsync(hotelId),
                AverageRating = await _repo.GetHotelAverageRatingAsync(hotelId)
            };
        }

        public async Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId)
        {
            return new GuestDashboardDto
            {
                TotalBookings = await _repo.GetGuestReservationsAsync(userId),
                ActiveBookings = await _repo.GetGuestActiveReservationsAsync(userId),
                CompletedBookings = await _repo.GetGuestCompletedReservationsAsync(userId),
                CancelledBookings = await _repo.GetGuestCancelledReservationsAsync(userId),
                TotalSpent = await _repo.GetGuestTotalSpentAsync(userId)
            };
        }

        public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
        {
            return new SuperAdminDashboardDto
            {
                TotalHotels = await _repo.GetTotalHotelsAsync(),
                TotalUsers = await _repo.GetTotalUsersAsync(),
                TotalReservations = await _repo.GetTotalReservationsAsync(),
                TotalRevenue = await _repo.GetTotalRevenueAsync(),
                TotalReviews = await _repo.GetTotalReviewsAsync()
            };
        }
    }
}
