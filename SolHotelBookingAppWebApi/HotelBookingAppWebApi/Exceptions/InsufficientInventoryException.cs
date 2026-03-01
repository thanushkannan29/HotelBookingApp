namespace HotelBookingAppWebApi.Exceptions
{
    public class InsufficientInventoryException:Exception
    {
        public InsufficientInventoryException(string message) : base($"{message} is Inventory insufficient")
        {
        }
    }
}
