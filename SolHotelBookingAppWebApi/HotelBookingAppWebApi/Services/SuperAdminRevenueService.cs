using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Revenue;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class SuperAdminRevenueService : ISuperAdminRevenueService
    {
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

        /// <summary>Called inline when admin marks a reservation as Completed.</summary>
        public async Task RecordCommissionAsync(Guid reservationId)
        {
            // Idempotent — skip if already recorded
            var alreadyExists = await _revenueRepo.GetQueryable()
                .AnyAsync(r => r.ReservationId == reservationId);
            if (alreadyExists) return;

            var reservation = await _reservationRepo.GetAsync(reservationId)
                ?? throw new NotFoundException("Reservation not found.");

            var commission = Math.Round(reservation.TotalAmount * 0.02M, 2);
            await _revenueRepo.AddAsync(new SuperAdminRevenue
            {
                SuperAdminRevenueId = Guid.NewGuid(),
                ReservationId = reservation.ReservationId,
                HotelId = reservation.HotelId,
                ReservationAmount = reservation.TotalAmount,
                CommissionAmount = commission,
                SuperAdminUpiId = "thanushstayhubsuperadmin@okaxis",
                CreatedAt = DateTime.UtcNow
            });

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
            var total = await _revenueRepo.GetQueryable().SumAsync(r => (decimal?)r.CommissionAmount) ?? 0;
            return new RevenueSummaryDto { TotalCommissionEarned = total };
        }

        private static SuperAdminRevenueDto MapToDto(SuperAdminRevenue r) => new()
        {
            SuperAdminRevenueId = r.SuperAdminRevenueId,
            ReservationCode = r.Reservation?.ReservationCode ?? string.Empty,
            HotelName = r.Hotel?.Name ?? string.Empty,
            ReservationAmount = r.ReservationAmount,
            CommissionAmount = r.CommissionAmount,
            SuperAdminUpiId = r.SuperAdminUpiId,
            CreatedAt = r.CreatedAt
        };
    }
}
