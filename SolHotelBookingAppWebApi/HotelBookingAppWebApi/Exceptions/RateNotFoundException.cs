namespace HotelBookingAppWebApi.Exceptions
{
    public class RateNotFoundException:Exception
    {
        public RateNotFoundException(string message) : base($"{message} is Rate not Found")
        {
        }
    }
}
