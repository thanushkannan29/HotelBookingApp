using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class AmenityRequestService : IAmenityRequestService
    {
        private readonly IRepository<Guid, AmenityRequest> _requestRepo;
        private readonly IRepository<Guid, Amenity> _amenityRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IUnitOfWork _unitOfWork;

        public AmenityRequestService(
            IRepository<Guid, AmenityRequest> requestRepo,
            IRepository<Guid, Amenity> amenityRepo,
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IUnitOfWork unitOfWork)
        {
            _requestRepo = requestRepo;
            _amenityRepo = amenityRepo;
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<AmenityRequestResponseDto> CreateRequestAsync(Guid adminUserId, CreateAmenityRequestDto dto)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (admin.HotelId == null)
                throw new ValidationException("Admin has no associated hotel.");

            var hotel = await _hotelRepo.GetAsync(admin.HotelId.Value)
                ?? throw new NotFoundException("Hotel not found.");

            var request = new AmenityRequest
            {
                AmenityRequestId = Guid.NewGuid(),
                RequestedByAdminId = adminUserId,
                AdminHotelId = admin.HotelId.Value,
                AmenityName = dto.AmenityName,
                Category = dto.Category,
                IconName = dto.IconName,
                Status = AmenityRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _requestRepo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(request, admin.Name, hotel.Name);
        }

        public async Task<IEnumerable<AmenityRequestResponseDto>> GetAdminRequestsAsync(Guid adminUserId)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            var requests = await _requestRepo.GetQueryable()
                .Where(r => r.RequestedByAdminId == adminUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var hotel = admin.HotelId.HasValue
                ? await _hotelRepo.GetAsync(admin.HotelId.Value)
                : null;

            return requests.Select(r => MapToDto(r, admin.Name, hotel?.Name ?? string.Empty));
        }

        public async Task<PagedAmenityRequestResponseDto> GetAdminRequestsPagedAsync(Guid adminUserId, int page, int pageSize, string? search = null)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            var query = _requestRepo.GetQueryable()
                .Where(r => r.RequestedByAdminId == adminUserId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(r =>
                    r.AmenityName.ToLower().Contains(s) ||
                    r.Category.ToLower().Contains(s));
            }

            query = query.OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var hotel = admin.HotelId.HasValue
                ? await _hotelRepo.GetAsync(admin.HotelId.Value)
                : null;

            return new PagedAmenityRequestResponseDto
            {
                TotalCount = total,
                Requests = items.Select(r => MapToDto(r, admin.Name, hotel?.Name ?? string.Empty))
            };
        }

        public async Task<PagedAmenityRequestResponseDto> GetAllRequestsAsync(string? status, int page, int pageSize)
        {
            var query = _requestRepo.GetQueryable()
                .Include(r => r.RequestedByAdmin)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                if (Enum.TryParse<AmenityRequestStatus>(status, out var statusEnum))
                    query = query.Where(r => r.Status == statusEnum);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var hotelIds = items.Select(r => r.AdminHotelId).Distinct().ToList();
            var hotels = await _hotelRepo.GetQueryable()
                .Where(h => hotelIds.Contains(h.HotelId))
                .ToDictionaryAsync(h => h.HotelId, h => h.Name);

            return new PagedAmenityRequestResponseDto
            {
                TotalCount = total,
                Requests = items.Select(r => MapToDto(
                    r,
                    r.RequestedByAdmin?.Name ?? string.Empty,
                    hotels.TryGetValue(r.AdminHotelId, out var hn) ? hn : string.Empty))
            };
        }

        public async Task<AmenityRequestResponseDto> ApproveRequestAsync(Guid requestId, Guid superAdminUserId)
        {
            var request = await _requestRepo.GetAsync(requestId)
                ?? throw new NotFoundException("Request not found.");

            if (request.Status != AmenityRequestStatus.Pending)
                throw new ValidationException("Request is not pending.");

            // Insert into Amenities table
            var amenity = new Amenity
            {
                AmenityId = Guid.NewGuid(),
                Name = request.AmenityName,
                Category = request.Category,
                IconName = request.IconName,
                IsActive = true
            };

            await _amenityRepo.AddAsync(amenity);

            request.Status = AmenityRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            var admin = await _userRepo.GetAsync(request.RequestedByAdminId);
            var hotel = await _hotelRepo.GetAsync(request.AdminHotelId);

            return MapToDto(request, admin?.Name ?? string.Empty, hotel?.Name ?? string.Empty);
        }

        public async Task<AmenityRequestResponseDto> RejectRequestAsync(Guid requestId, Guid superAdminUserId, string note)
        {
            var request = await _requestRepo.GetAsync(requestId)
                ?? throw new NotFoundException("Request not found.");

            if (request.Status != AmenityRequestStatus.Pending)
                throw new ValidationException("Request is not pending.");

            request.Status = AmenityRequestStatus.Rejected;
            request.SuperAdminNote = note;
            request.ProcessedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            var admin = await _userRepo.GetAsync(request.RequestedByAdminId);
            var hotel = await _hotelRepo.GetAsync(request.AdminHotelId);

            return MapToDto(request, admin?.Name ?? string.Empty, hotel?.Name ?? string.Empty);
        }

        private static AmenityRequestResponseDto MapToDto(AmenityRequest r, string adminName, string hotelName) => new()
        {
            AmenityRequestId = r.AmenityRequestId,
            AmenityName = r.AmenityName,
            Category = r.Category,
            IconName = r.IconName,
            Status = r.Status.ToString(),
            SuperAdminNote = r.SuperAdminNote,
            AdminName = adminName,
            HotelName = hotelName,
            CreatedAt = r.CreatedAt,
            ProcessedAt = r.ProcessedAt
        };
    }
}
