namespace HotelBookingAppWebApi.Exceptions
{
    public class ValidationException:Exception
    {
        public ValidationException(string message) : base($"{message} is Validation Error")
        {
        }
    }
}
