using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using HotelBookingAppWebApi.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class AmenityService : IAmenityService
    {
        private readonly IRepository<Guid, Amenity> _amenityRepo;
        private readonly HotelBookingContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public AmenityService(IRepository<Guid, Amenity> amenityRepo, HotelBookingContext context, IUnitOfWork unitOfWork)
        {
            _amenityRepo = amenityRepo;
            _context = context;
            _unitOfWork = unitOfWork;
        }

        // ── GET ALL ACTIVE ────────────────────────────────────────────────────
        public async Task<IEnumerable<AmenityResponseDto>> GetAllActiveAsync()
        {
            return await _amenityRepo.GetQueryable()
                .Where(a => a.IsActive)
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        // ── SEARCH ────────────────────────────────────────────────────────────
        public async Task<IEnumerable<AmenityResponseDto>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<AmenityResponseDto>();

            return await _amenityRepo.GetQueryable()
                .Where(a => a.IsActive && a.Name.ToLower().Contains(query.ToLower()))
                .OrderBy(a => a.Category)
                .ThenBy(a => a.Name)
                .Take(20)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        // ── CREATE (SuperAdmin) ───────────────────────────────────────────────
        public async Task<AmenityResponseDto> CreateAmenityAsync(CreateAmenityDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var exists = await _amenityRepo.GetQueryable()
                    .AnyAsync(a => a.Name.ToLower() == dto.Name.ToLower());

                if (exists)
                    throw new ConflictException("An amenity with this name already exists.");

                var amenity = new Amenity
                {
                    AmenityId = Guid.NewGuid(),
                    Name = dto.Name,
                    Category = dto.Category,
                    IconName = dto.IconName,
                    IsActive = true
                };

                await _amenityRepo.AddAsync(amenity);
                await _unitOfWork.CommitAsync();

                return MapToDto(amenity);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── UPDATE (SuperAdmin) ───────────────────────────────────────────────
        public async Task<AmenityResponseDto> UpdateAmenityAsync(UpdateAmenityDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var amenity = await _amenityRepo.GetAsync(dto.AmenityId)
                    ?? throw new NotFoundException("Amenity not found.");

                amenity.Name = dto.Name;
                amenity.Category = dto.Category;
                amenity.IconName = dto.IconName;
                amenity.IsActive = dto.IsActive;

                await _amenityRepo.UpdateAsync(dto.AmenityId, amenity);
                await _unitOfWork.CommitAsync();

                return MapToDto(amenity);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private static AmenityResponseDto MapToDto(Amenity a) => new()
        {
            AmenityId = a.AmenityId,
            Name = a.Name,
            Category = a.Category,
            IconName = a.IconName,
            IsActive = a.IsActive
        };

        // ── GET ALL PAGED (SuperAdmin) ─────────────────────────────────────────
        public async Task<PagedAmenityResponseDto> GetAllAmenitiesPagedAsync(int page, int pageSize, string? search, string? category)
        {
            var query = _amenityRepo.GetQueryable().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Name.ToLower().Contains(search.ToLower()) ||
                                         a.Category.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                query = query.Where(a => a.Category == category);

            var total = await query.CountAsync();
            var amenities = await query
                .OrderBy(a => a.Category).ThenBy(a => a.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => MapToDto(a))
                .ToListAsync();

            return new PagedAmenityResponseDto { TotalCount = total, Amenities = amenities };
        }

        // ── TOGGLE STATUS (SuperAdmin) ─────────────────────────────────────────
        public async Task<bool> ToggleAmenityStatusAsync(Guid amenityId)
        {
            var amenity = await _amenityRepo.GetAsync(amenityId)
                ?? throw new NotFoundException("Amenity not found.");

            amenity.IsActive = !amenity.IsActive;
            await _amenityRepo.UpdateAsync(amenityId, amenity);
            await _unitOfWork.SaveChangesAsync();

            return amenity.IsActive;
        }

        // ── DELETE (SuperAdmin) ────────────────────────────────────────────────
        public async Task<bool> DeleteAmenityAsync(Guid amenityId)
        {
            var amenity = await _amenityRepo.GetAsync(amenityId)
                ?? throw new NotFoundException("Amenity not found.");

            var inUse = await _context.RoomTypeAmenities
                .AnyAsync(rta => rta.AmenityId == amenityId);

            if (inUse)
                throw new ConflictException("Amenity is in use by one or more room types.");

            await _amenityRepo.DeleteAsync(amenityId);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}