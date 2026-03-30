namespace HotelBookingAppWebApi.Models.DTOs.PromoCode
{
    public class PagedPromoCodeResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<PromoCodeResponseDto> PromoCodes { get; set; } = new List<PromoCodeResponseDto>();
    }
}
