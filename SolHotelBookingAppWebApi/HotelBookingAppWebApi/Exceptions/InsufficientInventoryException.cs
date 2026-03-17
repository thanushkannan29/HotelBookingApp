namespace HotelBookingAppWebApi.Exceptions
{
    public class InsufficientInventoryException : AppException
    {
        public InsufficientInventoryException(string message)
            : base($"{message} - Inventory insufficient", 409) { }
    }
}
