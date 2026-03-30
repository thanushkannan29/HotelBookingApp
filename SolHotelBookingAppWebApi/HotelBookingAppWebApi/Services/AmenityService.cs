using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages the global amenity catalogue — CRUD for SuperAdmin and read-only for public.
    /// Prevents deletion of amenities that are in use by room types.
    /// </summary>
    public class AmenityService : IAmenityService
    {
        private readonly IRepository<Guid, Amenity> _amenityRepo;
        private readonly HotelBookingContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public AmenityService(
            IRepository<Guid, Amenity> amenityRepo,
            HotelBookingContext context,
            IUnitOfWork unitOfWork)
        {
            _amenityRepo = amenityRepo;
            _context = context;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<IEnumerable<AmenityResponseDto>> GetAllActiveAsync()
            => await _amenityRepo.GetQueryable()
                .Where(a => a.IsActive)
                .OrderBy(a => a.Category).ThenBy(a => a.Name)
                .Select(a => MapToDto(a))
                .ToListAsync();

        public async Task<IEnumerable<AmenityResponseDto>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Enumerable.Empty<AmenityResponseDto>();

            return await _amenityRepo.GetQueryable()
                .Where(a => a.IsActive && a.Name.ToLower().Contains(query.ToLower()))
                .OrderBy(a => a.Category).ThenBy(a => a.Name)
                .Take(20)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        public async Task<PagedAmenityResponseDto> GetAllAmenitiesPagedAsync(
            int page, int pageSize, string? search, string? category)
        {
            var query = BuildFilteredQuery(search, category);
            var total = await query.CountAsync();
            var amenities = await query
                .OrderBy(a => a.Category).ThenBy(a => a.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => MapToDto(a))
                .ToListAsync();

            return new PagedAmenityResponseDto { TotalCount = total, Amenities = amenities };
        }

        public async Task<AmenityResponseDto> CreateAmenityAsync(CreateAmenityDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await EnsureNameIsUniqueAsync(dto.Name);
                var amenity = BuildAmenity(dto);
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

        public async Task<AmenityResponseDto> UpdateAmenityAsync(UpdateAmenityDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var amenity = await GetAmenityOrThrowAsync(dto.AmenityId);
                ApplyUpdates(amenity, dto);
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

        public async Task<bool> ToggleAmenityStatusAsync(Guid amenityId)
        {
            var amenity = await GetAmenityOrThrowAsync(amenityId);
            amenity.IsActive = !amenity.IsActive;
            await _amenityRepo.UpdateAsync(amenityId, amenity);
            await _unitOfWork.SaveChangesAsync();
            return amenity.IsActive;
        }

        public async Task<bool> DeleteAmenityAsync(Guid amenityId)
        {
            var amenity = await GetAmenityOrThrowAsync(amenityId);
            await EnsureNotInUseAsync(amenityId);
            await _amenityRepo.DeleteAsync(amenityId);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<Amenity> GetAmenityOrThrowAsync(Guid amenityId)
            => await _amenityRepo.GetAsync(amenityId)
                ?? throw new NotFoundException("Amenity not found.");

        private async Task EnsureNameIsUniqueAsync(string name)
        {
            var exists = await _amenityRepo.GetQueryable()
                .AnyAsync(a => a.Name.ToLower() == name.ToLower());
            if (exists) throw new ConflictException("An amenity with this name already exists.");
        }

        private async Task EnsureNotInUseAsync(Guid amenityId)
        {
            var inUse = await _context.RoomTypeAmenities.AnyAsync(rta => rta.AmenityId == amenityId);
            if (inUse) throw new ConflictException("Amenity is in use by one or more room types.");
        }

        private IQueryable<Amenity> BuildFilteredQuery(string? search, string? category)
        {
            var query = _amenityRepo.GetQueryable().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a =>
                    a.Name.ToLower().Contains(search.ToLower()) ||
                    a.Category.ToLower().Contains(search.ToLower()));
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
                query = query.Where(a => a.Category == category);
            return query;
        }

        private static Amenity BuildAmenity(CreateAmenityDto dto) => new()
        {
            AmenityId = Guid.NewGuid(),
            Name = dto.Name,
            Category = dto.Category,
            IconName = dto.IconName,
            IsActive = true
        };

        private static void ApplyUpdates(Amenity amenity, UpdateAmenityDto dto)
        {
            amenity.Name = dto.Name;
            amenity.Category = dto.Category;
            amenity.IconName = dto.IconName;
            amenity.IsActive = dto.IsActive;
        }

        private static AmenityResponseDto MapToDto(Amenity amenity) => new()
        {
            AmenityId = amenity.AmenityId,
            Name = amenity.Name,
            Category = amenity.Category,
            IconName = amenity.IconName,
            IsActive = amenity.IsActive
        };
    }
}
