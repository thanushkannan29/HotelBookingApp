using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
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

        public async Task<IEnumerable<PromoCodeResponseDto>> GetGuestPromoCodesAsync(Guid userId)
        {
            var promos = await _promoRepo.GetQueryable()
                .Include(p => p.Hotel)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return promos.Select(MapToDto);
        }

        public async Task<PagedPromoCodeResponseDto> GetGuestPromoCodesPagedAsync(Guid userId, int page, int pageSize)
        {
            var query = _promoRepo.GetQueryable()
                .Include(p => p.Hotel)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedPromoCodeResponseDto
            {
                TotalCount = total,
                PromoCodes = items.Select(MapToDto)
            };
        }

        public async Task<PromoCodeValidationResultDto> ValidateAsync(Guid userId, ValidatePromoCodeDto dto)
        {
            var promo = await _promoRepo.GetQueryable()
                .FirstOrDefaultAsync(p =>
                    p.Code == dto.Code &&
                    p.UserId == userId &&
                    p.HotelId == dto.HotelId);

            if (promo == null)
                return Invalid("Promo code not found or not applicable to this hotel.");

            if (promo.IsUsed)
                return Invalid("Promo code has already been used.");

            if (promo.ExpiryDate < DateTime.UtcNow)
                return Invalid("Promo code has expired.");

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

            if (reservation == null) return;

            // Check if promo already generated for this reservation
            var exists = await _promoRepo.GetQueryable()
                .AnyAsync(p => p.ReservationId == reservationId);
            if (exists) return;

            var discountPercent = CalculateDiscountPercent(reservation.TotalAmount);

            var promo = new PromoCode
            {
                PromoCodeId = Guid.NewGuid(),
                Code = GenerateCode(),
                UserId = reservation.UserId,
                HotelId = reservation.HotelId,
                ReservationId = reservationId,
                DiscountPercent = discountPercent,
                ExpiryDate = DateTime.UtcNow.AddDays(90),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _promoRepo.AddAsync(promo);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MarkUsedAsync(string code, Guid userId)
        {
            var promo = await _promoRepo.GetQueryable()
                .FirstOrDefaultAsync(p => p.Code == code && p.UserId == userId);

            if (promo != null)
            {
                promo.IsUsed = true;
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────
        private static decimal CalculateDiscountPercent(decimal totalAmount) => totalAmount switch
        {
            <= 500 => 5,
            <= 1000 => 10,
            <= 2000 => 15,
            <= 5000 => 20,
            _ => 25
        };

        private static string GenerateCode()
            => $"PROMO-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        private static PromoCodeValidationResultDto Invalid(string msg) => new()
        {
            IsValid = false,
            Message = msg
        };

        private static PromoCodeResponseDto MapToDto(PromoCode p)
        {
            var now = DateTime.UtcNow;
            string status = p.IsUsed ? "Used" : p.ExpiryDate < now ? "Expired" : "Active";
            return new PromoCodeResponseDto
            {
                PromoCodeId = p.PromoCodeId,
                Code = p.Code,
                HotelName = p.Hotel?.Name ?? string.Empty,
                HotelId = p.HotelId,
                DiscountPercent = p.DiscountPercent,
                ExpiryDate = p.ExpiryDate,
                IsUsed = p.IsUsed,
                Status = status
            };
        }
    }
}
