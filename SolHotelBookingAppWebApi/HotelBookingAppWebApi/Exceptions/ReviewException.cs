namespace HotelBookingAppWebApi.Exceptions
{
    public class ReviewException : AppException
    {
        public ReviewException(string message)
            : base(message, 400) { }
    }
}
