using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces.Repository;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Repository
{
    public class ReviewRepository : Repository<Guid, Review>, IReviewRepository
    {
        public ReviewRepository(HotelBookingContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetReviewsByHotelAsync(Guid hotelId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.HotelId == hotelId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetReviewsByUserAsync(Guid userId)
        {
            return await _context.Reviews
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }
    }
}
