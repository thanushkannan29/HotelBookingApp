using HotelBookingAppWebApi.Models.DTOs.PromoCode;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Promo code generation, validation, and lifecycle management.</summary>
    public interface IPromoCodeService
    {
        /// <summary>Returns all promo codes for the guest (no pagination).</summary>
        Task<IEnumerable<PromoCodeResponseDto>> GetGuestPromoCodesAsync(Guid userId);

        /// <summary>Returns paged promo codes for the guest with optional status filter (Active/Used/Expired).</summary>
        Task<PagedPromoCodeResponseDto> GetGuestPromoCodesPagedAsync(
            Guid userId, int page, int pageSize, string? status = null);

        /// <summary>Validates a promo code for a specific hotel and booking amount.</summary>
        Task<PromoCodeValidationResultDto> ValidateAsync(Guid userId, ValidatePromoCodeDto dto);

        /// <summary>Auto-generates a loyalty promo code when a reservation is completed. Idempotent.</summary>
        Task GeneratePromoForCompletedReservationAsync(Guid reservationId);

        /// <summary>Marks a promo code as used after a successful booking.</summary>
        Task MarkUsedAsync(string code, Guid userId);
    }
}
