using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.City;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class CityService : ICityService
    {
        private readonly IRepository<Guid, City> _cityRepo;
        private readonly IUnitOfWork _unitOfWork;

        public CityService(IRepository<Guid, City> cityRepo, IUnitOfWork unitOfWork)
        {
            _cityRepo = cityRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<CityDto>> SearchCitiesAsync(string? search)
        {
            var query = _cityRepo.GetQueryable()
                .Where(c => c.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.CityName.StartsWith(search));

            return await query
                .OrderBy(c => c.CityName)
                .Take(10)
                .Select(c => MapToDto(c))
                .ToListAsync();
        }

        public async Task<IEnumerable<CityDto>> GetAllActiveCitiesAsync()
        {
            return await _cityRepo.GetQueryable()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CityName)
                .Select(c => MapToDto(c))
                .ToListAsync();
        }

        public async Task<PagedCityResponseDto> GetAllCitiesPagedAsync(int page, int pageSize, string? search)
        {
            var query = _cityRepo.GetQueryable().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.CityName.Contains(search) || c.StateName.Contains(search));

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.CityName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => MapToDto(c))
                .ToListAsync();

            return new PagedCityResponseDto { TotalCount = total, Cities = items };
        }

        public async Task<CityDto> AddCityAsync(CreateCityDto dto)
        {
            var exists = await _cityRepo.GetQueryable()
                .AnyAsync(c => c.CityName.ToLower() == dto.CityName.ToLower() &&
                               c.StateName.ToLower() == dto.StateName.ToLower());

            if (exists) throw new ConflictException("City already exists.");

            var city = new City
            {
                CityId = Guid.NewGuid(),
                CityName = dto.CityName,
                StateName = dto.StateName,
                PinCode = dto.PinCode,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _cityRepo.AddAsync(city);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(city);
        }

        public async Task<CityDto> UpdateCityAsync(Guid cityId, UpdateCityDto dto)
        {
            var city = await _cityRepo.GetAsync(cityId)
                ?? throw new NotFoundException("City not found.");

            city.CityName = dto.CityName;
            city.StateName = dto.StateName;
            city.PinCode = dto.PinCode;

            await _unitOfWork.SaveChangesAsync();
            return MapToDto(city);
        }

        public async Task<bool> ToggleCityStatusAsync(Guid cityId)
        {
            var city = await _cityRepo.GetAsync(cityId)
                ?? throw new NotFoundException("City not found.");

            city.IsActive = !city.IsActive;
            await _unitOfWork.SaveChangesAsync();
            return city.IsActive;
        }

        public async Task<bool> DeleteCityAsync(Guid cityId)
        {
            var city = await _cityRepo.GetAsync(cityId)
                ?? throw new NotFoundException("City not found.");

            await _cityRepo.DeleteAsync(cityId);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static CityDto MapToDto(City c) => new()
        {
            CityId = c.CityId,
            CityName = c.CityName,
            StateName = c.StateName,
            PinCode = c.PinCode,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        };
    }
}
