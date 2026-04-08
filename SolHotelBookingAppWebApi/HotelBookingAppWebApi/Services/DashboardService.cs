using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Aggregates statistics for Admin, Guest, and SuperAdmin dashboards.
    /// Each dashboard method is self-contained and queries only what it needs.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, Transaction> _transactionRepo;
        private readonly IRepository<Guid, Review> _reviewRepo;
        private readonly IRepository<Guid, Room> _roomRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, SuperAdminRevenue> _revenueRepo;

        public DashboardService(
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Review> reviewRepo,
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, SuperAdminRevenue> revenueRepo)
        {
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _reservationRepo = reservationRepo;
            _transactionRepo = transactionRepo;
            _reviewRepo = reviewRepo;
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _revenueRepo = revenueRepo;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId)
        {
            var hotelId = await GetAdminHotelIdOrThrowAsync(userId);
            var hotel = await _hotelRepo.GetAsync(hotelId)
                ?? throw new NotFoundException("Hotel not found.");

            var roomStats = await GetRoomStatsAsync(hotelId);
            var reservationStats = await GetReservationStatsAsync(hotelId);
            var totalRevenue = await GetHotelRevenueAsync(hotelId);
            var reviewStats = await GetReviewStatsAsync(hotelId);

            return new AdminDashboardDto
            {
                HotelId = hotelId,
                HotelName = hotel.Name,
                HotelImageUrl = hotel.ImageUrl,
                IsActive = hotel.IsActive,
                IsBlockedBySuperAdmin = hotel.IsBlockedBySuperAdmin,
                TotalRooms = roomStats.Total,
                ActiveRooms = roomStats.Active,
                TotalRoomTypes = roomStats.Types,
                TotalReservations = reservationStats.Total,
                PendingReservations = reservationStats.Pending,
                ActiveReservations = reservationStats.Confirmed,
                CompletedReservations = reservationStats.Completed,
                CancelledReservations = reservationStats.Cancelled,
                TotalRevenue = totalRevenue,
                TotalReviews = reviewStats.Count,
                AverageRating = reviewStats.Average
            };
        }

        public async Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId)
        {
            var resQuery = _reservationRepo.GetQueryable().Where(r => r.UserId == userId);
            var totalSpent = await GetGuestSpendAsync(userId);

            return new GuestDashboardDto
            {
                TotalBookings = await resQuery.CountAsync(),
                ActiveBookings = await resQuery.CountAsync(r => r.Status == ReservationStatus.Confirmed),
                CompletedBookings = await resQuery.CountAsync(r => r.Status == ReservationStatus.Completed),
                CancelledBookings = await resQuery.CountAsync(r => r.Status == ReservationStatus.Cancelled),
                TotalSpent = totalSpent
            };
        }

        public async Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync()
        {
            // Platform revenue = sum of 2% commissions recorded in SuperAdminRevenue.
            // This is the actual money earned by the platform, NOT the total booking amounts.
            var totalRevenue = await _revenueRepo.GetQueryable()
                .SumAsync(r => (decimal?)r.CommissionAmount) ?? 0;

            return new SuperAdminDashboardDto
            {
                TotalHotels = await _hotelRepo.GetQueryable().CountAsync(),
                ActiveHotels = await _hotelRepo.GetQueryable().CountAsync(h => h.IsActive),
                BlockedHotels = await _hotelRepo.GetQueryable().CountAsync(h => h.IsBlockedBySuperAdmin),
                TotalUsers = await _userRepo.GetQueryable().CountAsync(),
                TotalReservations = await _reservationRepo.GetQueryable().CountAsync(),
                TotalRevenue = totalRevenue,
                TotalReviews = await _reviewRepo.GetQueryable().CountAsync()
            };
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<Guid> GetAdminHotelIdOrThrowAsync(Guid userId)
        {
            var hotelId = await _userRepo.GetQueryable()
                .Where(u => u.UserId == userId)
                .Select(u => u.HotelId)
                .FirstOrDefaultAsync();

            if (hotelId is null) throw new NotFoundException("Admin hotel not found.");
            return hotelId.Value;
        }

        private async Task<(int Total, int Active, int Types)> GetRoomStatsAsync(Guid hotelId)
        {
            var total = await _roomRepo.GetQueryable().CountAsync(r => r.HotelId == hotelId);
            var active = await _roomRepo.GetQueryable().CountAsync(r => r.HotelId == hotelId && r.IsActive);
            var types = await _roomTypeRepo.GetQueryable().CountAsync(rt => rt.HotelId == hotelId);
            return (total, active, types);
        }

        private async Task<(int Total, int Pending, int Confirmed, int Completed, int Cancelled)>
            GetReservationStatsAsync(Guid hotelId)
        {
            var q = _reservationRepo.GetQueryable().Where(r => r.HotelId == hotelId);
            return (
                await q.CountAsync(),
                await q.CountAsync(r => r.Status == ReservationStatus.Pending),
                await q.CountAsync(r => r.Status == ReservationStatus.Confirmed),
                await q.CountAsync(r => r.Status == ReservationStatus.Completed),
                await q.CountAsync(r => r.Status == ReservationStatus.Cancelled)
            );
        }

        private async Task<decimal> GetHotelRevenueAsync(Guid hotelId)
            // Only count revenue from Completed reservations — these are fully earned.
            // Pending/Confirmed reservations may still be cancelled and refunded.
            => await _transactionRepo.GetQueryable()
                .Where(t => t.Status == PaymentStatus.Success
                         && t.Reservation!.HotelId == hotelId
                         && t.Reservation.Status == ReservationStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

        private async Task<(int Count, decimal Average)> GetReviewStatsAsync(Guid hotelId)
        {
            var q = _reviewRepo.GetQueryable().Where(r => r.HotelId == hotelId);
            var count = await q.CountAsync();
            var avg = count > 0 ? Math.Round(await q.AverageAsync(r => (decimal?)r.Rating) ?? 0, 2) : 0;
            return (count, avg);
        }

        private async Task<decimal> GetGuestSpendAsync(Guid userId)
            // Sum FinalAmount of Completed reservations only.
            // Completed = stay is done, money is fully earned by the hotel — no refund possible.
            // Cancelled reservations are excluded entirely (refunds already returned to wallet).
            // Pending/Confirmed are excluded — payment may still be reversed.
            => await _reservationRepo.GetQueryable()
                .Where(r => r.UserId == userId && r.Status == ReservationStatus.Completed)
                .SumAsync(r => (decimal?)r.FinalAmount) ?? 0;
    }
}
