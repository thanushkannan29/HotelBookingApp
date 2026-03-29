using HotelBookingAppWebApi.Models.DTOs.PromoCode;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IPromoCodeService
    {
        Task<IEnumerable<PromoCodeResponseDto>> GetGuestPromoCodesAsync(Guid userId);
        Task<PagedPromoCodeResponseDto> GetGuestPromoCodesPagedAsync(Guid userId, int page, int pageSize, string? status = null);
        Task<PromoCodeValidationResultDto> ValidateAsync(Guid userId, ValidatePromoCodeDto dto);
        Task GeneratePromoForCompletedReservationAsync(Guid reservationId);
        Task MarkUsedAsync(string code, Guid userId);
    }
}
