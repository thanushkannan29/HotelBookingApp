namespace HotelBookingAppWebApi.Exceptions
{
    public class UnableToCreateEntityException : AppException
    {
        public UnableToCreateEntityException(string message = "Unable to create entity")
            : base(message, 400) { }
    }
}
