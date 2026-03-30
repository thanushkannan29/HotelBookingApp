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
    /// The signing key is loaded once at construction time from configuration.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _signingKey;
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(1);

        public TokenService(IConfiguration configuration)
        {
            var secret = configuration["Keys:Jwt"]
                ?? throw new InvalidOperationException("JWT Key not configured.");
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public string CreateToken(TokenPayloadDto payload)
        {
            var claims = BuildClaims(payload);
            var descriptor = BuildTokenDescriptor(claims);
            return WriteToken(descriptor);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private static List<Claim> BuildClaims(TokenPayloadDto payload)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
                new(ClaimTypes.Name,           payload.UserName),
                new(ClaimTypes.Role,           payload.Role)
            };

            if (payload.HotelId.HasValue)
                claims.Add(new Claim("HotelId", payload.HotelId.ToString()!));

            return claims;
        }

        private SecurityTokenDescriptor BuildTokenDescriptor(IEnumerable<Claim> claims) => new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        private static string WriteToken(SecurityTokenDescriptor descriptor)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }
    }
}
