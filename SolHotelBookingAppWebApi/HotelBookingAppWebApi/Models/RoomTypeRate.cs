namespace HotelBookingAppWebApi.Models
{
    public class RoomTypeRate
    {
        public int RoomTypeRateId { get; set; }

        public int RoomTypeId { get; set; }
        public RoomType? RoomType { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public decimal Rate { get; set; }

        public override string ToString()
        {
            return $"RoomTypeRate [{RoomTypeRateId}] | RoomTypeId: {RoomTypeId} | " +
                   $"From: {StartDate:yyyy-MM-dd} To: {EndDate:yyyy-MM-dd} | Rate: {Rate:C}";
        }

    }
}
