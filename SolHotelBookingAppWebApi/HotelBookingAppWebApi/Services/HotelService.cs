using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class HotelService : IHotelService
    {
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IUnitOfWork _unitOfWork;

        public HotelService(
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, User> userRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IUnitOfWork unitOfWork)
        {
            _hotelRepo = hotelRepo;
            _userRepo = userRepo;
            _roomTypeRepo = roomTypeRepo;
            _unitOfWork = unitOfWork;
        }

        
        // TOP HOTELS
        
        public async Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync()
        {
            return await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive)
                .Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl,

                    AverageRating = h.Reviews.Any()
                        ? Math.Round(h.Reviews.Average(r => (decimal)r.Rating), 2)
                        : 0m,


                    ReviewCount = h.Reviews.Count(),

                    StartingPrice = h.RoomTypes
                        .SelectMany(rt => rt.Rates)
                        .Min(r => (decimal?)r.Rate) ?? 0
                })
                .OrderByDescending(h => h.AverageRating)
                .ThenByDescending(h => h.ReviewCount)
                .Take(10)
                .ToListAsync();
        }

        
        // SEARCH HOTELS
        
        public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
        {
            var query = _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive && h.City == request.City);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
                throw new NotFoundException("No hotels found");

            var hotels = await query
                .OrderBy(h => h.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl
                })
                .ToListAsync();

            return new SearchHotelResponseDto
            {
                Hotels = hotels,
                PageNumber = request.PageNumber,
                RecordsCount = totalRecords
            };
        }

        
        // HOTEL DETAILS
        
        public async Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Include(h => h.RoomTypes)
                .Include(h => h.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(h => h.HotelId == hotelId);

            if (hotel == null)
                throw new NotFoundException("Hotel not found");

            var reviews = hotel.Reviews ?? new List<Review>();

            var amenities = hotel.RoomTypes?
                .Where(rt => !string.IsNullOrEmpty(rt.Amenities))
                .SelectMany(rt => rt.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(a => a.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            return new HotelDetailsDto
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Address = hotel.Address,
                City = hotel.City,
                Description = hotel.Description,

                AverageRating = reviews.Any()
                    ? Math.Round(reviews.Average(r => (decimal)r.Rating), 2)
                    : 0m,


                Amenities = amenities,

                Reviews = reviews.Select(r => new ReviewDto
                {
                    UserName = r.User?.Name ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedDate = r.CreatedDate
                })
            };
        }

        
        // ROOM TYPES
        
        public async Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId)
        {
            var types = await _roomTypeRepo.GetQueryable()
                .AsNoTracking()
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .ToListAsync();

            return types.Select(t => new RoomTypePublicDto
            {
                RoomTypeId = t.RoomTypeId,
                Name = t.Name,
                Description = t.Description,
                MaxOccupancy = t.MaxOccupancy,
                Amenities = t.Amenities?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[] { }
            });
        }

        
        // AVAILABILITY
        
        public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(
            Guid hotelId,
            DateOnly checkIn,
            DateOnly checkOut)
        {
            var inventories = await _roomTypeRepo.GetQueryable()
                .AsNoTracking()
                .Where(rt => rt.HotelId == hotelId)
                .SelectMany(rt => rt.Inventories)
                .Where(i => i.Date >= checkIn && i.Date <= checkOut)
                .Include(i => i.RoomType!)
                    .ThenInclude(rt => rt.Rates)
                .ToListAsync();

            return inventories
                .GroupBy(i => i.RoomType!)
                .Select(g =>
                {
                    var rate = g.Key.Rates?
                        .FirstOrDefault(r =>
                            checkIn >= r.StartDate &&
                            checkIn <= r.EndDate);

                    return new RoomAvailabilityDto
                    {
                        RoomTypeId = g.Key.RoomTypeId,
                        RoomTypeName = g.Key.Name,
                        PricePerNight = rate?.Rate ?? 0,
                        AvailableRooms = g.Min(x => x.AvailableInventory)
                    };
                });
        }

        
        // UPDATE HOTEL (WITH TRANSACTION)
        
        public async Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null || user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized");

                var hotel = await _hotelRepo.GetAsync(user.HotelId.Value);

                if (hotel == null)
                    throw new NotFoundException("Hotel not found");

                hotel.Name = dto.Name;
                hotel.Address = dto.Address;
                hotel.City = dto.City;
                hotel.Description = dto.Description;
                hotel.ContactNumber = dto.ContactNumber;
                hotel.ImageUrl = dto.ImageUrl;

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        
        // TOGGLE HOTEL STATUS (WITH TRANSACTION)
        
        public async Task ToggleHotelStatusAsync(Guid userId, bool isActive)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null || user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized");

                var hotel = await _hotelRepo.GetAsync(user.HotelId.Value);

                if (hotel == null)
                    throw new NotFoundException("Hotel not found");

                hotel.IsActive = isActive;

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
