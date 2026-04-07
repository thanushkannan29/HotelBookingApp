using HotelBookingAppWebApi.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// HMAC-SHA256 password hashing.
    /// Pass <c>null</c> for <paramref name="existingSalt"/> on registration to generate a new salt.
    /// Pass the stored salt on login to reproduce the same hash for comparison.
    /// </summary>
    public class PasswordService : IPasswordService
    {
        /// <inheritdoc/>
        public byte[] HashPassword(string password, byte[]? existingSalt, out byte[]? newSalt)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);

            using var hmac = CreateHmac(existingSalt, out newSalt);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private static HMACSHA256 CreateHmac(byte[]? existingSalt, out byte[]? newSalt)
        {
            if (existingSalt is null)
            {
                var hmac = new HMACSHA256();
                newSalt = hmac.Key;
                return hmac;
            }

            newSalt = null;
            return new HMACSHA256(existingSalt);
        }
    }
}
