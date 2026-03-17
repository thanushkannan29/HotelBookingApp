using HotelBookingAppWebApi.Contexts;
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

        // ✅ ADMIN DASHBOARD
        public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId)
        {
            var user = await _userRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.HotelId == null)
                throw new NotFoundException("Admin hotel not found");

            var hotelId = user.HotelId.Value;

            var totalRooms = await _roomRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId)
                .CountAsync();

            var totalReservations = await _reservationRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId)
                .CountAsync();

            var activeReservations = await _reservationRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId &&
                            r.Status == ReservationStatus.Confirmed)
                .CountAsync();

            var totalRevenue = await _transactionRepo.GetQueryable()
                .Include(t => t.Reservation)
                .Where(t => t.Reservation!.HotelId == hotelId &&
                            t.Status == PaymentStatus.Success)
                .SumAsync(t => t.Amount);

            var totalReviews = await _reviewRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId)
                .CountAsync();

            var reviewQuery = _reviewRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId);

            var averageRating = await reviewQuery.AnyAsync()
                ? await reviewQuery.AverageAsync(r => r.Rating)
                : 0;

            return new AdminDashboardDto
            {
                HotelId = hotelId,
                TotalRooms = totalRooms,
                TotalReservations = totalReservations,
                ActiveReservations = activeReservations,
                TotalRevenue = totalRevenue,
                TotalReviews = totalReviews,
                AverageRating = averageRating
            };
        }

        // ✅ GUEST DASHBOARD
        public async Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId)
        {
            var totalBookings = await _reservationRepo.GetQueryable()
                .Where(r => r.UserId == userId)
                .CountAsync();

            var activeBookings = await _reservationRepo.GetQueryable()
                .Where(r => r.UserId == userId &&
                            r.Status == ReservationStatus.Confirmed)
                .CountAsync();

            var completedBookings = await _reservationRepo.GetQueryable()
                .Where(r => r.UserId == userId &&
                            r.Status == ReservationStatus.Completed)
                .CountAsync();

            var cancelledBookings = await _reservationRepo.GetQueryable()
                .Where(r => r.UserId == userId &&
                            r.Status == ReservationStatus.Cancelled)
                .CountAsync();

            var totalSpent = await _transactionRepo.GetQueryable()
                .Include(t => t.Reservation)
                .Where(t => t.Reservation!.UserId == userId &&
                            t.Status == PaymentStatus.Success)
                .SumAsync(t => t.Amount);

            return new GuestDashboardDto
            {
                TotalBookings = totalBookings,
                ActiveBookings = activeBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalSpent = totalSpent
            };
        }

        // ✅ SUPER ADMIN DASHBOARD
        public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
        {
            var totalHotels = await _hotelRepo.GetQueryable().CountAsync();
            var totalUsers = await _userRepo.GetQueryable().CountAsync();
            var totalReservations = await _reservationRepo.GetQueryable().CountAsync();

            var totalRevenue = await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success)
                .SumAsync(t => t.Amount);

            var totalReviews = await _reviewRepo.GetQueryable().CountAsync();

            return new SuperAdminDashboardDto
            {
                TotalHotels = totalHotels,
                TotalUsers = totalUsers,
                TotalReservations = totalReservations,
                TotalRevenue = totalRevenue,
                TotalReviews = totalReviews
            };
        }
    }
}
