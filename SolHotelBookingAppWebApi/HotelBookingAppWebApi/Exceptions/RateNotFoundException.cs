namespace HotelBookingAppWebApi.Exceptions
{
    public class RateNotFoundException : AppException
    {
        public RateNotFoundException(string message)
            : base($"{message} - Rate not found", 404) { }
    }
}
