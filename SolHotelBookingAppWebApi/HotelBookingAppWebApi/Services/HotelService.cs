using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using Microsoft.EntityFrameworkCore;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Exceptions;
namespace HotelBookingAppWebApi.Services
{
    public class HotelService : IHotelService
    {
        private readonly HotelBookingContext _context;

        public HotelService(HotelBookingContext context)
        {
            _context = context;
        }

        // TOP 10 HOTELS FOR HOME PAGE

        public async Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync()
        {
            var result = await _context.TopHotelViews
                .FromSqlRaw("EXEC proc_GetTopHotels")
                .ToListAsync();

            return result.Select(h => new HotelListItemDto
            {
                HotelId = h.HotelId,
                Name = h.Name,
                City = h.City,
                ImageUrl = h.ImageUrl,
                AverageRating = h.AverageRating,
                ReviewCount = h.ReviewCount,
                StartingPrice = h.StartingPrice
            });
        }


        // PUBLIC SEARCH

        public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
        {
            var offset = (request.PageNumber - 1) * request.PageSize;

            var hotels = await _context.Hotels
                .FromSqlRaw("EXEC proc_SearchHotels {0},{1},{2},{3},{4}",
                request.City,
                offset,
                request.PageSize,
                request.CheckIn,
                request.CheckOut)
                .ToListAsync();

            if (!hotels.Any())
                throw new NotFoundException("No hotels found");

            return new SearchHotelResponseDto
            {
                Hotels = hotels.Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl
                }),
                PageNumber = request.PageNumber,
                RecordsCount = hotels.Count
            };
        }

        //  HOTEL DETAILS

        public async Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId)
        {
            var hotel = await _context.Hotels
                .Include(h => h.RoomTypes)
                .Include(h => h.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(h => h.HotelId == hotelId);

            if (hotel == null)
                throw new NotFoundException("Hotel not found");

            var reviews = hotel.Reviews ?? new List<Review>();

            //  Extract amenities from RoomTypes (string split)
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
                    ? Math.Round(reviews.Average(r => r.Rating), 2)
                    : 0,

                Amenities = amenities,

                Reviews = reviews
                    .OrderByDescending(r => r.CreatedDate)
                    .Select(r => new ReviewDto
                    {
                        UserName = r.User != null ? r.User.Name : "Anonymous",
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedDate = r.CreatedDate
                    })
                    .ToList()
            };
        }



        // ADMIN UPDATE

        public async Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null || user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized");

                var hotel = await _context.Hotels.FindAsync(user.HotelId);

                hotel!.Name = dto.Name;
                hotel.Address = dto.Address;
                hotel.City = dto.City;
                hotel.Description = dto.Description;
                hotel.ContactNumber = dto.ContactNumber;
                hotel.ImageUrl = dto.ImageUrl;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ACTIVATE / DEACTIVATE 

        public async Task ToggleHotelStatusAsync(Guid userId, bool isActive)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized");

            var hotel = await _context.Hotels.FindAsync(user.HotelId);
            hotel!.IsActive = isActive;

            await _context.SaveChangesAsync();
        }
    }
}
