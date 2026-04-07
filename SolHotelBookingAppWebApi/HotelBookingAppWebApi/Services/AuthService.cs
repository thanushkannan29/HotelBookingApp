using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Handles guest registration, hotel-admin registration, and login.
    /// Follows SRP — each public method delegates to focused private helpers.
    /// </summary>
    public class AuthService(
        IRepository<Guid, User> userRepository,
        IRepository<Guid, Hotel> hotelRepository,
        IRepository<Guid, UserProfileDetails> userProfileRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        IWalletService walletService,
        IUnitOfWork unitOfWork) : IAuthService
    {
        private readonly IRepository<Guid, User> _userRepository = userRepository;
        private readonly IRepository<Guid, Hotel> _hotelRepository = hotelRepository;
        private readonly IRepository<Guid, UserProfileDetails> _userProfileRepository = userProfileRepository;
        private readonly IPasswordService _passwordService = passwordService;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IWalletService _walletService = walletService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        // ── REGISTER GUEST ────────────────────────────────────────────────────

        public async Task<AuthResponseDto> RegisterGuestAsync(RegisterUserDto dto)
        {
            await EnsureEmailIsUniqueAsync(dto.Email);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await CreateGuestUserAsync(dto);
                await CreateUserProfileAsync(user.UserId, dto.Name, dto.Email);
                await _unitOfWork.CommitAsync();
                await _walletService.EnsureWalletExistsAsync(user.UserId);
                return BuildAuthResponse(user);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── REGISTER HOTEL ADMIN ──────────────────────────────────────────────

        public async Task<AuthResponseDto> RegisterHotelAdminAsync(RegisterHotelAdminDto dto)
        {
            await EnsureEmailIsUniqueAsync(dto.Email);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var hotel = await CreateHotelAsync(dto);
                var admin = await CreateAdminUserAsync(dto, hotel.HotelId);
                await CreateUserProfileAsync(admin.UserId, dto.Name, dto.Email, dto.Address, dto.City);
                await _unitOfWork.CommitAsync();
                return BuildAuthResponse(admin);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── LOGIN ─────────────────────────────────────────────────────────────

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.FirstOrDefaultAsync(u => u.Email == dto.Email)
                ?? throw new UnAuthorizedException("Invalid credentials.");

            EnsureAccountIsActive(user);
            VerifyPassword(dto.Password, user);

            return BuildAuthResponse(user);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task EnsureEmailIsUniqueAsync(string email)
        {
            var exists = await _userRepository.GetQueryable().AnyAsync(u => u.Email == email);
            if (exists) throw new ConflictException("Email already registered.");
        }

        private static void EnsureAccountIsActive(User user)
        {
            if (!user.IsActive) throw new UnAuthorizedException("Account is deactivated.");
        }

        private void VerifyPassword(string plainPassword, User user)
        {
            var hashed = _passwordService.HashPassword(plainPassword, user.PasswordSaltValue, out _);
            if (!hashed.SequenceEqual(user.Password))
                throw new UnAuthorizedException("Invalid credentials.");
        }

        private async Task<User> CreateGuestUserAsync(RegisterUserDto dto)
        {
            var hashedPassword = _passwordService.HashPassword(dto.Password, null, out var salt);
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
            return user;
        }

        private async Task<Hotel> CreateHotelAsync(RegisterHotelAdminDto dto)
        {
            var hotel = new Hotel
            {
                HotelId = Guid.NewGuid(),
                Name = dto.HotelName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Description = dto.Description,
                ContactNumber = dto.ContactNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _hotelRepository.AddAsync(hotel);
            return hotel;
        }

        private async Task<User> CreateAdminUserAsync(RegisterHotelAdminDto dto, Guid hotelId)
        {
            var hashedPassword = _passwordService.HashPassword(dto.Password, null, out var salt);
            var admin = new User
            {
                UserId = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                Password = hashedPassword,
                PasswordSaltValue = salt!,
                Role = UserRole.Admin,
                HotelId = hotelId,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddAsync(admin);
            return admin;
        }

        private async Task CreateUserProfileAsync(
            Guid userId, string name, string email,
            string address = "Not Updated", string city = "Not Updated")
        {
            var profile = new UserProfileDetails
            {
                UserDetailsId = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                Email = email,
                PhoneNumber = "Not Updated",
                Address = address,
                City = city,
                State = "Not Updated",
                Pincode = "000000",
                CreatedAt = DateTime.UtcNow
            };
            await _userProfileRepository.AddAsync(profile);
        }

        private AuthResponseDto BuildAuthResponse(User user)
        {
            var payload = new TokenPayloadDto
            {
                UserId = user.UserId,
                UserName = user.Name,
                Role = user.Role.ToString(),
                HotelId = user.HotelId
            };
            return new AuthResponseDto { Token = _tokenService.CreateToken(payload) };
        }
    }
}
