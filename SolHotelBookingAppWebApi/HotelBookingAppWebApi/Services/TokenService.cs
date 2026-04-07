using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Creates signed JWT tokens from a <see cref="TokenPayloadDto"/>.
    /// Claims use short JWT names so they are readable by both the frontend (jwtDecode)
    /// and the backend (with MapInboundClaims = false).
    /// </summary>
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
            var claims = BuildClaims(payload);
            var descriptor = BuildTokenDescriptor(claims);
            return WriteToken(descriptor);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private List<Claim> BuildClaims(TokenPayloadDto payload)
        {
            var claims = new List<Claim>
            {
                new("nameid",      payload.UserId.ToString()),
                new("unique_name", payload.UserName),
                new("role",        payload.Role)
            };

            if (payload.HotelId.HasValue)
                claims.Add(new Claim("HotelId", payload.HotelId.ToString()!));

            return claims;
        }

        private SecurityTokenDescriptor BuildTokenDescriptor(List<Claim> claims)
        {
            return new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
            };
        }

        private static string WriteToken(SecurityTokenDescriptor descriptor)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }
    }
}
