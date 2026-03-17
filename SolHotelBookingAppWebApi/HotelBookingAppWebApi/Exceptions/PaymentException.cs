namespace HotelBookingAppWebApi.Exceptions
{
    public class PaymentException : AppException
    {
        public PaymentException(string message)
            : base(message, 400) { }
    }
}
