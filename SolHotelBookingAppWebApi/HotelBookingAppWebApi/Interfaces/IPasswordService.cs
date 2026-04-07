namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>HMAC-SHA256 password hashing with per-user salt.</summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Hashes <paramref name="password"/> using HMAC-SHA256.
        /// Pass <c>null</c> for <paramref name="existingSalt"/> on registration — a new salt is generated and returned via <paramref name="newSalt"/>.
        /// Pass the stored salt on login — <paramref name="newSalt"/> will be <c>null</c>.
        /// </summary>
        byte[] HashPassword(string password, byte[]? existingSalt, out byte[]? newSalt);
    }
}
