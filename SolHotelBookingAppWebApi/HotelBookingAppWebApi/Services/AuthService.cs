
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Auth;

namespace HotelBookingAppWebApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<Guid, User> _userRepository;
        private readonly IRepository<Guid, Hotel> _hotelRepository;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthService(
            IRepository<Guid, User> userRepository,
            IRepository<Guid, Hotel> hotelRepository,
            IPasswordService passwordService,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _hotelRepository = hotelRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        // ================= REGISTER GUEST =================

        public async Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto)
        {
            var existingUsers = await _userRepository.FindAsync(u => u.Email == dto.Email);
            if (existingUsers.Any())
                throw new Exception("Email already registered");

            byte[]? salt;
            var hashedPassword = _passwordService.HashPassword(dto.Password, null, out salt);

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                Password = hashedPassword,
                PasswordSaltValue = salt!,
                Role = UserRole.Guest,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            return GenerateToken(user);
        }

        // ================= REGISTER HOTEL ADMIN =================

        public async Task<AuthResponseDto> RegisterHotelAdminAsync(RegisterHotelAdminDto dto)
        {
            // Check if email already exists
            var existing = await _userRepository.FindAsync(u => u.Email == dto.Email);
            if (existing.Any())
                throw new Exception("Email already registered");

            // 1️⃣ Create Hotel FIRST
            var hotel = new Hotel
            {
                HotelId = Guid.NewGuid(),
                Name = dto.HotelName,
                Address = dto.Address,
                City = dto.City,
                Description = dto.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _hotelRepository.AddAsync(hotel);

            // 2️⃣ Create Admin linked to hotel
            byte[]? salt;
            var hashedPassword = _passwordService.HashPassword(dto.Password, null, out salt);

            var admin = new User
            {
                UserId = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                Password = hashedPassword,
                PasswordSaltValue = salt!,
                Role = UserRole.Admin,
                HotelId = hotel.HotelId,   // Now we assign generated hotel id
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(admin);

            // 3️⃣ Generate Token
            return GenerateToken(admin);
        }


        // ================= LOGIN =================

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var users = await _userRepository.FindAsync(u => u.Email == dto.Email);
            var user = users.FirstOrDefault();

            if (user == null)
                throw new Exception("Invalid credentials");

            var hashed = _passwordService.HashPassword(dto.Password, user.PasswordSaltValue, out _);

            if (!hashed.SequenceEqual(user.Password))
                throw new Exception("Invalid credentials");

            return GenerateToken(user);
        }

        // ================= TOKEN CREATION =================

        private AuthResponseDto GenerateToken(User user)
        {
            var payload = new TokenPayloadDto
            {
                UserId = user.UserId,
                UserName = user.Name,
                Role = user.Role.ToString(),
                HotelId = user.HotelId
            };

            var token = _tokenService.CreateToken(payload);

            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(1)
            };
        }
    }
}
