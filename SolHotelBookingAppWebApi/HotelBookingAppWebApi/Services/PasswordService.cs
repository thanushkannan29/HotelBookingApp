using HotelBookingAppWebApi.Interfaces;
using System.Security.Cryptography;

namespace HotelBookingAppWebApi.Services
{
    public class PasswordService : IPasswordService
    {
        public byte[] HashPassword(string password, byte[]? dbHashKey, out byte[]? hashkey)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            HMACSHA256 hmac;
            if (dbHashKey == null)
            {
                hmac = new HMACSHA256();
                hashkey = hmac.Key;
            }
            else
            {
                hmac = new HMACSHA256(dbHashKey);
                hashkey = null;
            }

            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            return hmac.ComputeHash(passwordBytes);
        }
    }
}
