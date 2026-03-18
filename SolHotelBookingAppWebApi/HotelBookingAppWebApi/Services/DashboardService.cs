using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, Transaction> _transactionRepo;
        private readonly IRepository<Guid, Review> _reviewRepo;
        private readonly IRepository<Guid, Room> _roomRepo;

        public DashboardService(
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Review> reviewRepo,
            IRepository<Guid, Room> roomRepo)
        {
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _reservationRepo = reservationRepo;
            _transactionRepo = transactionRepo;
            _reviewRepo = reviewRepo;
            _roomRepo = roomRepo;
        }

        //  ADMIN DASHBOARD (OPTIMIZED)
        public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId)
        {
            var user = await _userRepo.GetQueryable()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.HotelId })
                .FirstOrDefaultAsync();

            if (user == null || user.HotelId == null)
                throw new NotFoundException("Admin hotel not found");

            var hotelId = user.HotelId.Value;

            var totalRooms = await _roomRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId)
                .CountAsync();

            var reservationQuery = _reservationRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId);

            var totalReservations = await reservationQuery.CountAsync();

            var activeReservations = await reservationQuery
                .Where(r => r.Status == ReservationStatus.Confirmed)
                .CountAsync();

            var totalRevenue = await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success &&
                            t.Reservation!.HotelId == hotelId)
                .SumAsync(t => (decimal?)t.Amount);

            var reviewQuery = _reviewRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId);

            var totalReviews = await reviewQuery.CountAsync();

            var averageRating = totalReviews > 0
                ? await reviewQuery.AverageAsync(r => (decimal?)r.Rating)
                : 0;

            return new AdminDashboardDto
            {
                HotelId = hotelId,
                TotalRooms = totalRooms,
                TotalReservations = totalReservations,
                ActiveReservations = activeReservations,
                TotalRevenue = totalRevenue ?? 0,
                TotalReviews = totalReviews,
                AverageRating = averageRating ?? 0
            };
        }



        //  GUEST DASHBOARD (OPTIMIZED)
        public async Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId)
        {
            var reservationQuery = _reservationRepo.GetQueryable()
                .Where(r => r.UserId == userId);

            var totalBookings = await reservationQuery.CountAsync();

            var activeBookings = await reservationQuery
                .Where(r => r.Status == ReservationStatus.Confirmed)
                .CountAsync();

            var completedBookings = await reservationQuery
                .Where(r => r.Status == ReservationStatus.Completed)
                .CountAsync();

            var cancelledBookings = await reservationQuery
                .Where(r => r.Status == ReservationStatus.Cancelled)
                .CountAsync();

            var totalSpent = await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success &&
                            t.Reservation!.UserId == userId)
                .SumAsync(t => (decimal?)t.Amount);

            return new GuestDashboardDto
            {
                TotalBookings = totalBookings,
                ActiveBookings = activeBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalSpent = totalSpent ?? 0
            };
        }


        //  SUPER ADMIN DASHBOARD (OPTIMIZED)
        public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
        {
            var totalHotels = await _hotelRepo.GetQueryable().CountAsync();
            var totalUsers = await _userRepo.GetQueryable().CountAsync();
            var totalReservations = await _reservationRepo.GetQueryable().CountAsync();

            var totalRevenue = await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success)
                .SumAsync(t => (decimal?)t.Amount);

            var totalReviews = await _reviewRepo.GetQueryable().CountAsync();

            return new SuperAdminDashboardDto
            {
                TotalHotels = totalHotels,
                TotalUsers = totalUsers,
                TotalReservations = totalReservations,
                TotalRevenue = totalRevenue ?? 0,
                TotalReviews = totalReviews
            };
        }

    }
}
