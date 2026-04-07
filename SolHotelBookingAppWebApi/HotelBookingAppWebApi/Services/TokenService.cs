using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelBookingAppWebApi.Services
{
<<<<<<< Updated upstream
=======
    /// <summary>
    /// Creates signed JWT tokens from a <see cref="TokenPayloadDto"/>.
    /// Claims use short JWT names so they are readable by both the frontend (jwtDecode)
    /// and the backend (with MapInboundClaims = false).
    /// </summary>
>>>>>>> Stashed changes
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _key;

        public TokenService(IConfiguration configuration)
        {
            string secret = configuration["Keys:Jwt"]
                ?? throw new InvalidOperationException("JWT Key not configured.");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        public string CreateToken(TokenPayloadDto payload)
        {
<<<<<<< Updated upstream
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
                new Claim(ClaimTypes.Name, payload.UserName),
                new Claim(ClaimTypes.Role, payload.Role)
=======
            var claims = BuildClaims(payload);
            var descriptor = BuildTokenDescriptor(claims);
            return WriteToken(descriptor);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private static List<Claim> BuildClaims(TokenPayloadDto payload)
        {
            // Use short JWT claim names so they round-trip correctly with
            // MapInboundClaims = false and are readable by the Angular frontend.
            var claims = new List<Claim>
            {
                new("nameid",       payload.UserId.ToString()),
                new("unique_name",  payload.UserName),
                new("role",         payload.Role)
>>>>>>> Stashed changes
            };

            if (payload.HotelId.HasValue)
                claims.Add(new Claim("HotelId", payload.HotelId.ToString()!));

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = creds
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(tokenDescriptor));
        }
    }
}
