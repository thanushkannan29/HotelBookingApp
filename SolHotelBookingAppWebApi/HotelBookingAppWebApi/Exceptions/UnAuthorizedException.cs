namespace HotelBookingAppWebApi.Exceptions
{
    public class UnAuthorizedException : AppException
    {
        public UnAuthorizedException(string message = "Unauthorized")
            : base(message, 401) { }
    }
}
