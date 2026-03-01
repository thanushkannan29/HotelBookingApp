namespace HotelBookingAppWebApi.Exceptions
{
    public class ReservationFailedException:Exception
    {
        public ReservationFailedException(string message) : base($"{message} is Reservation Failer try Again")
        {
        }
    }
}
