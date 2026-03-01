namespace HotelBookingAppWebApi.Exceptions
{
    public class PaymentException : Exception
    {
        public PaymentException(string message) : base(message) { }
    }
}
