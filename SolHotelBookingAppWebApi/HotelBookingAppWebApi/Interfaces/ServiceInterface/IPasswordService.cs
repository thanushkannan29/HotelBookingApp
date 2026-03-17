namespace HotelBookingAppWebApi.Interfaces
{
    public interface IPasswordService
    {
        byte[] HashPassword(string password, byte[]? existingSalt, out byte[]? newSalt);
    }
}
