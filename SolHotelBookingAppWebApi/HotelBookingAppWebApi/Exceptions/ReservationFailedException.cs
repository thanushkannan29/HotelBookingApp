namespace HotelBookingAppWebApi.Exceptions
{
    public class ReservationFailedException : AppException
    {
        public ReservationFailedException(string message)
            : base($"{message} - Reservation failed", 400) { }
    }
}
