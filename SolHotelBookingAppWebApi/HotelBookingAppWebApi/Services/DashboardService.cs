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
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, RefundRequest> _refundRepo;

        public DashboardService(
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Review> reviewRepo,
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RefundRequest> refundRepo)
        {
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _reservationRepo = reservationRepo;
            _transactionRepo = transactionRepo;
            _reviewRepo = reviewRepo;
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _refundRepo = refundRepo;
        }

        // ── ADMIN DASHBOARD ───────────────────────────────────────────────────
        public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId)
        {
            var user = await _userRepo.GetQueryable()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.HotelId })
                .FirstOrDefaultAsync();

            if (user == null || user.HotelId == null)
                throw new NotFoundException("Admin hotel not found.");

            var hotelId = user.HotelId.Value;

            var hotel = await _hotelRepo.GetAsync(hotelId)
                ?? throw new NotFoundException("Hotel not found.");

            var totalRooms = await _roomRepo.GetQueryable()
                .CountAsync(r => r.HotelId == hotelId);

            var activeRooms = await _roomRepo.GetQueryable()
                .CountAsync(r => r.HotelId == hotelId && r.IsActive);

            var totalRoomTypes = await _roomTypeRepo.GetQueryable()
                .CountAsync(rt => rt.HotelId == hotelId);

            var resQuery = _reservationRepo.GetQueryable().Where(r => r.HotelId == hotelId);

            var totalReservations = await resQuery.CountAsync();
            var pendingReservations = await resQuery.CountAsync(r => r.Status == ReservationStatus.Pending);
            var activeReservations = await resQuery.CountAsync(r => r.Status == ReservationStatus.Confirmed);
            var completedReservations = await resQuery.CountAsync(r => r.Status == ReservationStatus.Completed);
            var cancelledReservations = await resQuery.CountAsync(r => r.Status == ReservationStatus.Cancelled);

            var totalRevenue = await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success && t.Reservation!.HotelId == hotelId)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var reviewQuery = _reviewRepo.GetQueryable().Where(r => r.HotelId == hotelId);
            var totalReviews = await reviewQuery.CountAsync();
            var averageRating = totalReviews > 0
                ? await reviewQuery.AverageAsync(r => (decimal?)r.Rating) ?? 0 : 0;

            var pendingRefunds = await _refundRepo.GetQueryable()
                .CountAsync(r => r.Reservation!.HotelId == hotelId &&
                                 r.Status == RefundRequestStatus.Pending);

            return new AdminDashboardDto
            {
                HotelId = hotelId,
                HotelName = hotel.Name,
                HotelImageUrl = hotel.ImageUrl,
                IsActive = hotel.IsActive,
                IsBlockedBySuperAdmin = hotel.IsBlockedBySuperAdmin,
                TotalRooms = totalRooms,
                ActiveRooms = activeRooms,
                TotalRoomTypes = totalRoomTypes,
                TotalReservations = totalReservations,
                PendingReservations = pendingReservations,
                ActiveReservations = activeReservations,
                CompletedReservations = completedReservations,
                CancelledReservations = cancelledReservations,
                TotalRevenue = totalRevenue,
                TotalReviews = totalReviews,
                AverageRating = Math.Round(averageRating, 2),
            };
        }

        // ── GUEST DASHBOARD ───────────────────────────────────────────────────
        public async Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId)
        {
            var resQuery = _reservationRepo.GetQueryable().Where(r => r.UserId == userId);

            var totalBookings = await resQuery.CountAsync();
            var activeBookings = await resQuery.CountAsync(r => r.Status == ReservationStatus.Confirmed);
            var completedBookings = await resQuery.CountAsync(r => r.Status == ReservationStatus.Completed);
            var cancelledBookings = await resQuery.CountAsync(r => r.Status == ReservationStatus.Cancelled);

            var totalSpent = await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success && t.Reservation!.UserId == userId)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            return new GuestDashboardDto
            {
                TotalBookings = totalBookings,
                ActiveBookings = activeBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalSpent = totalSpent,
            };
        }

        // ── SUPERADMIN DASHBOARD ──────────────────────────────────────────────
        public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
        {
            var totalHotels = await _hotelRepo.GetQueryable().CountAsync();
            var activeHotels = await _hotelRepo.GetQueryable().CountAsync(h => h.IsActive);
            var blockedHotels = await _hotelRepo.GetQueryable().CountAsync(h => h.IsBlockedBySuperAdmin);
            var totalUsers = await _userRepo.GetQueryable().CountAsync();
            var totalReservations = await _reservationRepo.GetQueryable().CountAsync();

            var totalRevenue = await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalReviews = await _reviewRepo.GetQueryable().CountAsync();

            return new SuperAdminDashboardDto
            {
                TotalHotels = totalHotels,
                ActiveHotels = activeHotels,
                BlockedHotels = blockedHotels,
                TotalUsers = totalUsers,
                TotalReservations = totalReservations,
                TotalRevenue = totalRevenue,
                TotalReviews = totalReviews,
            };
        }
    }
}
