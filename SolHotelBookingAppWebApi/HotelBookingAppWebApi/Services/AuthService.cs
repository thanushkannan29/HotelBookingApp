using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<Guid, User> _userRepository;
        private readonly IRepository<Guid, Hotel> _hotelRepository;
        private readonly IRepository<Guid, UserProfileDetails> _userProfileRepository;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthService(
            IRepository<Guid, User> userRepository,
            IRepository<Guid, Hotel> hotelRepository,
            IRepository<Guid, UserProfileDetails> userProfileRepository,
            IPasswordService passwordService,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _hotelRepository = hotelRepository;
            _userProfileRepository = userProfileRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        // REGISTER GUEST
        public async Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto)
        {
            var exists = await _userRepository.GetQueryable().AnyAsync(u => u.Email == dto.Email);

            if (exists)
                throw new ConflictException("Email already registered");



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

            // CREATE USER PROFILE DETAILS
            var profile = new UserProfileDetails
            {
                UserDetailsId = Guid.NewGuid(),
                UserId = user.UserId,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = "Not Updated",
                Address = "Not Updated",
                City = "Not Updated",
                State = "Not Updated",
                Pincode = "000000",
                CreatedAt = DateTime.UtcNow
            };

            await _userProfileRepository.AddAsync(profile);

            return GenerateToken(user);
        }

        // REGISTER HOTEL ADMIN
        public async Task<AuthResponseDto> RegisterHotelAdminAsync(RegisterHotelAdminDto dto)
        {
            var exists = await _userRepository.GetQueryable().AnyAsync(u => u.Email == dto.Email);

            if (exists)
                throw new ConflictException("Email already registered");



            // Create Hotel
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

            // Create Admin
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
                HotelId = hotel.HotelId,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(admin);

            // Create Profile
            var profile = new UserProfileDetails
            {
                UserDetailsId = Guid.NewGuid(),
                UserId = admin.UserId,
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumber = "Not Updated",
                Address = dto.Address,
                City = dto.City,
                State = "Not Updated",
                Pincode = "000000",
                CreatedAt = DateTime.UtcNow
            };

            await _userProfileRepository.AddAsync(profile);

            return GenerateToken(admin);
        }

        // LOGIN
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                throw new UnAuthorizedException("Invalid credentials");


            var hashed = _passwordService.HashPassword(dto.Password, user.PasswordSaltValue, out _);

            if (!hashed.SequenceEqual(user.Password))
                throw new UnAuthorizedException("Invalid credentials");

            return GenerateToken(user);
        }

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
                Token = token
                //Expiration = DateTime.UtcNow.AddDays(1)
                //need to do this as token object in response in future
            };
        }
    }
}
