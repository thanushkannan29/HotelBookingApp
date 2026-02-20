using System.Transactions;

namespace HotelBookingAppWebApi.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        public string ReservationCode { get; set; } = Guid.NewGuid().ToString();

        public int UserId { get; set; }
        public User? User { get; set; }

        public int HotelId { get; set; }
        public Hotel? Hotel { get; set; }

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        public decimal TotalAmount { get; set; }

        public ReservationStatus Status { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<ReservationRoom> ReservationRooms { get; set; } = new List<ReservationRoom>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        public override string ToString()
        {
            return $"Reservation [{ReservationId}] | Code: {ReservationCode} | UserId: {UserId} | HotelId: {HotelId} | " +
                   $"CheckIn: {CheckInDate:yyyy-MM-dd} | CheckOut: {CheckOutDate:yyyy-MM-dd} | Status: {Status} | Total: {TotalAmount:C}";
        }



    }

    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }
}
