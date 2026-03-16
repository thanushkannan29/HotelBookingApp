using HotelBookingAppWebApi.Models;

namespace HotelBookingAppWebApi.Interfaces.Repository
{
    public interface IReviewRepository : IRepository<Guid, Review>
    {
        Task<IEnumerable<Review>> GetReviewsByHotelAsync(Guid hotelId);
        Task<IEnumerable<Review>> GetReviewsByUserAsync(Guid userId);
    }
}
