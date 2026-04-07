using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages amenity requests submitted by hotel admins and reviewed by SuperAdmin.
    /// </summary>
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

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<AmenityRequestResponseDto> CreateRequestAsync(
            Guid adminUserId, CreateAmenityRequestDto dto)
        {
            var (admin, hotel) = await GetAdminWithHotelAsync(adminUserId);

            var request = BuildNewRequest(adminUserId, admin.HotelId!.Value, dto);
            await _requestRepo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(request, admin.Name, hotel.Name);
        }

        public async Task<IEnumerable<AmenityRequestResponseDto>> GetAdminRequestsAsync(Guid adminUserId)
        {
            var admin = await GetUserOrThrowAsync(adminUserId);
            var requests = await _requestRepo.GetQueryable()
                .Where(r => r.RequestedByAdminId == adminUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var hotel = admin.HotelId.HasValue
                ? await _hotelRepo.GetAsync(admin.HotelId.Value)
                : null;

            return requests.Select(r => MapToDto(r, admin.Name, hotel?.Name ?? string.Empty));
        }

        public async Task<PagedAmenityRequestResponseDto> GetAdminRequestsPagedAsync(
            Guid adminUserId, int page, int pageSize, string? search = null)
        {
            var admin = await GetUserOrThrowAsync(adminUserId);
            var query = BuildAdminRequestQuery(adminUserId, search);

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

        public async Task<PagedAmenityRequestResponseDto> GetAllRequestsAsync(
            string? status, int page, int pageSize)
        {
            var query = BuildSuperAdminRequestQuery(status);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var hotelNames = await LoadHotelNamesAsync(items);

            return new PagedAmenityRequestResponseDto
            {
                TotalCount = total,
                Requests = items.Select(r => MapToDto(
                    r,
                    r.RequestedByAdmin?.Name ?? string.Empty,
                    hotelNames.TryGetValue(r.AdminHotelId, out var name) ? name : string.Empty))
            };
        }

        public async Task<AmenityRequestResponseDto> ApproveRequestAsync(
            Guid requestId, Guid superAdminUserId)
        {
            var request = await GetPendingRequestOrThrowAsync(requestId);
            await CreateApprovedAmenityAsync(request);

            request.Status = AmenityRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return await BuildResponseDtoAsync(request);
        }

        public async Task<AmenityRequestResponseDto> RejectRequestAsync(
            Guid requestId, Guid superAdminUserId, string note)
        {
            var request = await GetPendingRequestOrThrowAsync(requestId);

            request.Status = AmenityRequestStatus.Rejected;
            request.SuperAdminNote = note;
            request.ProcessedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return await BuildResponseDtoAsync(request);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<(User admin, Hotel hotel)> GetAdminWithHotelAsync(Guid adminUserId)
        {
            var admin = await GetUserOrThrowAsync(adminUserId);
            if (admin.HotelId is null)
                throw new ValidationException("Admin has no associated hotel.");
            var hotel = await _hotelRepo.GetAsync(admin.HotelId.Value)
                ?? throw new NotFoundException("Hotel not found.");
            return (admin, hotel);
        }

        private async Task<User> GetUserOrThrowAsync(Guid userId)
            => await _userRepo.GetAsync(userId) ?? throw new UnAuthorizedException("Unauthorized.");

        private async Task<AmenityRequest> GetPendingRequestOrThrowAsync(Guid requestId)
        {
            var request = await _requestRepo.GetAsync(requestId)
                ?? throw new NotFoundException("Request not found.");
            if (request.Status != AmenityRequestStatus.Pending)
                throw new ValidationException("Request is not pending.");
            return request;
        }

        private static AmenityRequest BuildNewRequest(
            Guid adminUserId, Guid hotelId, CreateAmenityRequestDto dto) => new()
        {
            AmenityRequestId = Guid.NewGuid(),
            RequestedByAdminId = adminUserId,
            AdminHotelId = hotelId,
            AmenityName = dto.AmenityName,
            Category = dto.Category,
            IconName = dto.IconName,
            Status = AmenityRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        private async Task CreateApprovedAmenityAsync(AmenityRequest request)
        {
            await _amenityRepo.AddAsync(new Amenity
            {
                AmenityId = Guid.NewGuid(),
                Name = request.AmenityName,
                Category = request.Category,
                IconName = request.IconName,
                IsActive = true
            });
        }

        private IQueryable<AmenityRequest> BuildAdminRequestQuery(Guid adminUserId, string? search)
        {
            var query = _requestRepo.GetQueryable()
                .Where(r => r.RequestedByAdminId == adminUserId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(r =>
                    r.AmenityName.ToLower().Contains(term) ||
                    r.Category.ToLower().Contains(term));
            }

            return query.OrderByDescending(r => r.CreatedAt);
        }

        private IQueryable<AmenityRequest> BuildSuperAdminRequestQuery(string? status)
        {
            var query = _requestRepo.GetQueryable()
                .Include(r => r.RequestedByAdmin)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All" &&
                Enum.TryParse<AmenityRequestStatus>(status, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);

            return query;
        }

        private async Task<Dictionary<Guid, string>> LoadHotelNamesAsync(
            IEnumerable<AmenityRequest> items)
        {
            var hotelIds = items.Select(r => r.AdminHotelId).Distinct().ToList();
            return await _hotelRepo.GetQueryable()
                .Where(h => hotelIds.Contains(h.HotelId))
                .ToDictionaryAsync(h => h.HotelId, h => h.Name);
        }

        private async Task<AmenityRequestResponseDto> BuildResponseDtoAsync(AmenityRequest request)
        {
            var admin = await _userRepo.GetAsync(request.RequestedByAdminId);
            var hotel = await _hotelRepo.GetAsync(request.AdminHotelId);
            return MapToDto(request, admin?.Name ?? string.Empty, hotel?.Name ?? string.Empty);
        }

        private static AmenityRequestResponseDto MapToDto(
            AmenityRequest request, string adminName, string hotelName) => new()
        {
            AmenityRequestId = request.AmenityRequestId,
            AmenityName = request.AmenityName,
            Category = request.Category,
            IconName = request.IconName,
            Status = request.Status.ToString(),
            SuperAdminNote = request.SuperAdminNote,
            AdminName = adminName,
            HotelName = hotelName,
            CreatedAt = request.CreatedAt,
            ProcessedAt = request.ProcessedAt
        };
    }
}
