namespace HotelBookingAppWebApi.Exceptions
{
    public class UserProfileException : AppException
    {
        public UserProfileException(string message)
            : base(message, 404) { }
    }
}
