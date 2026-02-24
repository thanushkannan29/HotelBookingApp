using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using Microsoft.EntityFrameworkCore;
using HotelBookingAppWebApi.Models;
namespace HotelBookingAppWebApi.Services
{
    public class HotelService : IHotelService
    {
        private readonly HotelBookingContext _context;

        public HotelService(HotelBookingContext context)
        {
            _context = context;
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
                throw new Exception("No hotels found");

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
                .Include(h => h.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(h => h.HotelId == hotelId);

            if (hotel == null)
                throw new Exception("Hotel not found");

            return new HotelDetailsDto
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Address = hotel.Address,
                City = hotel.City,
                Description = hotel.Description,
                Reviews = hotel.Reviews?.Select(r => new ReviewDto
                {
                    UserName = r.User!.Name,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedDate = r.CreatedDate
                }) ?? new List<ReviewDto>()
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
                    throw new Exception("Unauthorized");

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
                throw new Exception("Unauthorized");

            var hotel = await _context.Hotels.FindAsync(user.HotelId);
            hotel!.IsActive = isActive;

            await _context.SaveChangesAsync();
        }
    }
}
