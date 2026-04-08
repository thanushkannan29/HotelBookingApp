using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Revenue;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Records and retrieves the 2% platform commission earned by SuperAdmin
    /// on each completed reservation.
    /// </summary>
    public class SuperAdminRevenueService : ISuperAdminRevenueService
    {
        private const decimal CommissionRate = 0.02M;
        private const string SuperAdminUpiId = "thanushstayhubsuperadmin@okaxis";

        private readonly IRepository<Guid, SuperAdminRevenue> _revenueRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IUnitOfWork _unitOfWork;

        public SuperAdminRevenueService(
            IRepository<Guid, SuperAdminRevenue> revenueRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IUnitOfWork unitOfWork)
        {
            _revenueRepo = revenueRepo;
            _reservationRepo = reservationRepo;
            _hotelRepo = hotelRepo;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records a 2% commission for a completed reservation.
        /// Idempotent — safe to call multiple times for the same reservation.
        /// </summary>
        public async Task RecordCommissionAsync(Guid reservationId)
        {
            if (await CommissionAlreadyRecordedAsync(reservationId)) return;

            var reservation = await _reservationRepo.GetAsync(reservationId)
                ?? throw new NotFoundException("Reservation not found.");

            await _revenueRepo.AddAsync(BuildCommissionRecord(reservation));
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PagedRevenueResponseDto> GetAllRevenueAsync(int page, int pageSize)
        {
            var query = _revenueRepo.GetQueryable()
                .Include(r => r.Reservation)
                .Include(r => r.Hotel)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedRevenueResponseDto
            {
                TotalCount = total,
                Items = items.Select(MapToDto)
            };
        }

        public async Task<RevenueSummaryDto> GetSummaryAsync()
        {
            var total = await _revenueRepo.GetQueryable()
                .SumAsync(r => (decimal?)r.CommissionAmount) ?? 0;

            return new RevenueSummaryDto { TotalCommissionEarned = total };
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<bool> CommissionAlreadyRecordedAsync(Guid reservationId)
            => await _revenueRepo.GetQueryable().AnyAsync(r => r.ReservationId == reservationId);

        private static SuperAdminRevenue BuildCommissionRecord(Reservation reservation) => new()
        {
            SuperAdminRevenueId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            HotelId = reservation.HotelId,
            // ReservationAmount = the actual amount the guest paid (FinalAmount).
            // Commission is 2% of what was collected, not the pre-GST/pre-discount base.
            ReservationAmount = reservation.FinalAmount > 0 ? reservation.FinalAmount : reservation.TotalAmount,
            CommissionAmount = Math.Round(
                (reservation.FinalAmount > 0 ? reservation.FinalAmount : reservation.TotalAmount) * CommissionRate, 2),
            SuperAdminUpiId = SuperAdminUpiId,
            CreatedAt = DateTime.UtcNow
        };

        private static SuperAdminRevenueDto MapToDto(SuperAdminRevenue record) => new()
        {
            SuperAdminRevenueId = record.SuperAdminRevenueId,
            ReservationCode = record.Reservation?.ReservationCode ?? string.Empty,
            HotelName = record.Hotel?.Name ?? string.Empty,
            ReservationAmount = record.ReservationAmount,
            CommissionAmount = record.CommissionAmount,
            SuperAdminUpiId = record.SuperAdminUpiId,
            CreatedAt = record.CreatedAt
        };
    }
}
