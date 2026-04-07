using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages promo code generation, validation, and lifecycle.
    /// Codes are auto-generated on reservation completion and expire after 90 days.
    /// </summary>
    public class PromoCodeService : IPromoCodeService
    {
        private readonly IRepository<Guid, PromoCode> _promoRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PromoCodeService(
            IRepository<Guid, PromoCode> promoRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IUnitOfWork unitOfWork)
        {
            _promoRepo = promoRepo;
            _reservationRepo = reservationRepo;
            _hotelRepo = hotelRepo;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<IEnumerable<PromoCodeResponseDto>> GetGuestPromoCodesAsync(Guid userId)
        {
            var promos = await _promoRepo.GetQueryable()
                .Include(p => p.Hotel)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return promos.Select(MapToDto);
        }

        public async Task<PagedPromoCodeResponseDto> GetGuestPromoCodesPagedAsync(
            Guid userId, int page, int pageSize, string? status = null)
        {
            var query = BuildGuestPromoQuery(userId, status);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedPromoCodeResponseDto
            {
                TotalCount = total,
                PromoCodes = items.Select(MapToDto)
            };
        }

        public async Task<PromoCodeValidationResultDto> ValidateAsync(
            Guid userId, ValidatePromoCodeDto dto)
        {
            var promo = await _promoRepo.GetQueryable()
                .FirstOrDefaultAsync(p =>
                    p.Code == dto.Code &&
                    p.UserId == userId &&
                    p.HotelId == dto.HotelId);

            if (promo is null) return InvalidResult("Promo code not found or not applicable to this hotel.");
            if (promo.IsUsed) return InvalidResult("Promo code has already been used.");
            if (promo.ExpiryDate < DateTime.UtcNow) return InvalidResult("Promo code has expired.");

            var discountAmount = Math.Round(dto.TotalAmount * promo.DiscountPercent / 100, 2);
            return new PromoCodeValidationResultDto
            {
                IsValid = true,
                DiscountPercent = promo.DiscountPercent,
                DiscountAmount = discountAmount,
                Message = $"{promo.DiscountPercent}% discount applied — saving ₹{discountAmount}"
            };
        }

        public async Task GeneratePromoForCompletedReservationAsync(Guid reservationId)
        {
            var reservation = await _reservationRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);
            if (reservation is null) return;

            var alreadyExists = await _promoRepo.GetQueryable()
                .AnyAsync(p => p.ReservationId == reservationId);
            if (alreadyExists) return;

            var promo = BuildPromoCode(reservation);
            await _promoRepo.AddAsync(promo);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MarkUsedAsync(string code, Guid userId)
        {
            var promo = await _promoRepo.GetQueryable()
                .FirstOrDefaultAsync(p => p.Code == code && p.UserId == userId);

            if (promo is not null)
            {
                promo.IsUsed = true;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private IQueryable<PromoCode> BuildGuestPromoQuery(Guid userId, string? status)
        {
            var now = DateTime.UtcNow;
            var query = _promoRepo.GetQueryable()
                .Include(p => p.Hotel)
                .Where(p => p.UserId == userId)
                .AsQueryable();

            query = status switch
            {
                "Active"  => query.Where(p => !p.IsUsed && p.ExpiryDate >= now),
                "Used"    => query.Where(p => p.IsUsed),
                "Expired" => query.Where(p => !p.IsUsed && p.ExpiryDate < now),
                _         => query
            };

            return query.OrderByDescending(p => p.CreatedAt);
        }

        private static PromoCode BuildPromoCode(Reservation reservation) => new()
        {
            PromoCodeId = Guid.NewGuid(),
            Code = GenerateCode(),
            UserId = reservation.UserId,
            HotelId = reservation.HotelId,
            ReservationId = reservation.ReservationId,
            DiscountPercent = CalculateDiscountPercent(reservation.TotalAmount),
            ExpiryDate = DateTime.UtcNow.AddDays(90),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        private static decimal CalculateDiscountPercent(decimal totalAmount) => totalAmount switch
        {
            <= 500  => 5,
            <= 1000 => 10,
            <= 2000 => 15,
            <= 5000 => 20,
            _       => 25
        };

        private static string GenerateCode()
            => $"PROMO-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        private static PromoCodeValidationResultDto InvalidResult(string message) => new()
        {
            IsValid = false,
            Message = message
        };

        private static PromoCodeResponseDto MapToDto(PromoCode promo)
        {
            var now = DateTime.UtcNow;
            var status = promo.IsUsed ? "Used"
                : promo.ExpiryDate < now ? "Expired"
                : "Active";

            return new PromoCodeResponseDto
            {
                PromoCodeId = promo.PromoCodeId,
                Code = promo.Code,
                HotelName = promo.Hotel?.Name ?? string.Empty,
                HotelId = promo.HotelId,
                DiscountPercent = promo.DiscountPercent,
                ExpiryDate = promo.ExpiryDate,
                IsUsed = promo.IsUsed,
                Status = status
            };
        }
    }
}
