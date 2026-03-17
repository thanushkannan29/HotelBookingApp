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

            var totalRoomsTask = _roomRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId)
                .CountAsync();

            var reservationQuery = _reservationRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId);

            var totalReservationsTask = reservationQuery.CountAsync();

            var activeReservationsTask = reservationQuery
                .Where(r => r.Status == ReservationStatus.Confirmed)
                .CountAsync();

            var totalRevenueTask = _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success &&
                            t.Reservation!.HotelId == hotelId)
                .SumAsync(t => (decimal?)t.Amount);

            var reviewQuery = _reviewRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId);

            var totalReviewsTask = reviewQuery.CountAsync();

            //  FIXED
            var hasReviews = await reviewQuery.AnyAsync();

            var averageRatingTask = hasReviews
                ? reviewQuery.AverageAsync(r => (decimal?)r.Rating)
                : Task.FromResult<decimal?>(0);

            await Task.WhenAll(
                totalRoomsTask,
                totalReservationsTask,
                activeReservationsTask,
                totalRevenueTask,
                totalReviewsTask,
                averageRatingTask
            );

            return new AdminDashboardDto
            {
                HotelId = hotelId,
                TotalRooms = totalRoomsTask.Result,
                TotalReservations = totalReservationsTask.Result,
                ActiveReservations = activeReservationsTask.Result,
                TotalRevenue = totalRevenueTask.Result ?? 0,
                TotalReviews = totalReviewsTask.Result,
                AverageRating = averageRatingTask.Result ?? 0
            };
        }


        //  GUEST DASHBOARD (OPTIMIZED)
        public async Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId)
        {
            var reservationQuery = _reservationRepo.GetQueryable()
                .Where(r => r.UserId == userId);

            var totalBookingsTask = reservationQuery.CountAsync();

            var activeBookingsTask = reservationQuery
                .Where(r => r.Status == ReservationStatus.Confirmed)
                .CountAsync();

            var completedBookingsTask = reservationQuery
                .Where(r => r.Status == ReservationStatus.Completed)
                .CountAsync();

            var cancelledBookingsTask = reservationQuery
                .Where(r => r.Status == ReservationStatus.Cancelled)
                .CountAsync();

            var totalSpentTask = _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success &&
                            t.Reservation!.UserId == userId)
                .SumAsync(t => (decimal?)t.Amount);

            await Task.WhenAll(
                totalBookingsTask,
                activeBookingsTask,
                completedBookingsTask,
                cancelledBookingsTask,
                totalSpentTask
            );

            return new GuestDashboardDto
            {
                TotalBookings = totalBookingsTask.Result,
                ActiveBookings = activeBookingsTask.Result,
                CompletedBookings = completedBookingsTask.Result,
                CancelledBookings = cancelledBookingsTask.Result,
                TotalSpent = totalSpentTask.Result ?? 0
            };
        }

        //  SUPER ADMIN DASHBOARD (OPTIMIZED)
        public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
        {
            var totalHotelsTask = _hotelRepo.GetQueryable().CountAsync();
            var totalUsersTask = _userRepo.GetQueryable().CountAsync();
            var totalReservationsTask = _reservationRepo.GetQueryable().CountAsync();

            var totalRevenueTask = _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success)
                .SumAsync(t => (decimal?)t.Amount);

            var totalReviewsTask = _reviewRepo.GetQueryable().CountAsync();

            await Task.WhenAll(
                totalHotelsTask,
                totalUsersTask,
                totalReservationsTask,
                totalRevenueTask,
                totalReviewsTask
            );

            return new SuperAdminDashboardDto
            {
                TotalHotels = totalHotelsTask.Result,
                TotalUsers = totalUsersTask.Result,
                TotalReservations = totalReservationsTask.Result,
                TotalRevenue = totalRevenueTask.Result ?? 0,
                TotalReviews = totalReviewsTask.Result
            };
        }
    }
}
