namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class PagedTransactionResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<TransactionResponseDto> Transactions { get; set; } = new List<TransactionResponseDto>();
    }
}
