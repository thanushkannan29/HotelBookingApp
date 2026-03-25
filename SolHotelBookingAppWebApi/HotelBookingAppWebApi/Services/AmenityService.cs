using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class AmenityService : IAmenityService
    {
        private readonly IRepository<Guid, Amenity> _amenityRepo;
        private readonly IUnitOfWork _unitOfWork;

        public AmenityService(IRepository<Guid, Amenity> amenityRepo, IUnitOfWork unitOfWork)
        {
            _amenityRepo = amenityRepo;
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
    }
}