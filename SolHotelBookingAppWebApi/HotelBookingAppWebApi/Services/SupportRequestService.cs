using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly IRepository<Guid, SupportRequest> _repo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IUnitOfWork _unitOfWork;

        public SupportRequestService(
            IRepository<Guid, SupportRequest> repo,
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<SupportRequestResponseDto> CreatePublicRequestAsync(PublicSupportRequestDto dto)
        {
            var request = new SupportRequest
            {
                SupportRequestId = Guid.NewGuid(),
                GuestName = dto.Name,
                GuestEmail = dto.Email,
                Subject = dto.Subject,
                Message = dto.Message,
                Category = dto.Category,
                SubmitterRole = "Public",
                Status = SupportRequestStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(request, dto.Name, dto.Email, null);
        }

        public async Task<SupportRequestResponseDto> CreateGuestRequestAsync(Guid userId, GuestSupportRequestDto dto)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            Hotel? hotel = null;
            if (dto.HotelId.HasValue)
                hotel = await _hotelRepo.GetAsync(dto.HotelId.Value);

            var request = new SupportRequest
            {
                SupportRequestId = Guid.NewGuid(),
                UserId = userId,
                SubmitterRole = "Guest",
                Subject = dto.Subject,
                Message = dto.Message,
                Category = dto.Category,
                ReservationCode = dto.ReservationCode,
                HotelId = dto.HotelId,
                Status = SupportRequestStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(request, user.Name, user.Email, hotel?.Name);
        }

        public async Task<SupportRequestResponseDto> CreateAdminRequestAsync(Guid adminUserId, AdminSupportRequestDto dto)
        {
            var user = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            var request = new SupportRequest
            {
                SupportRequestId = Guid.NewGuid(),
                UserId = adminUserId,
                SubmitterRole = "Admin",
                Subject = dto.Subject,
                Message = dto.Message,
                Category = dto.Category,
                Status = SupportRequestStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(request, user.Name, user.Email, null);
        }

        public async Task<PagedSupportRequestResponseDto> GetGuestRequestsAsync(Guid userId, int page, int pageSize)
        {
            var query = _repo.GetQueryable()
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var user = await _userRepo.GetAsync(userId);
            return new PagedSupportRequestResponseDto
            {
                TotalCount = total,
                Requests = items.Select(r => MapToDto(r, user?.Name ?? string.Empty, user?.Email ?? string.Empty, r.Hotel?.Name))
            };
        }

        public async Task<PagedSupportRequestResponseDto> GetAdminRequestsAsync(Guid adminUserId, int page, int pageSize)
        {
            var query = _repo.GetQueryable()
                .Where(r => r.UserId == adminUserId)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var user = await _userRepo.GetAsync(adminUserId);
            return new PagedSupportRequestResponseDto
            {
                TotalCount = total,
                Requests = items.Select(r => MapToDto(r, user?.Name ?? string.Empty, user?.Email ?? string.Empty, null))
            };
        }

        public async Task<PagedSupportRequestResponseDto> GetAllRequestsAsync(
            string? status, string? role, string? search, int page, int pageSize)
        {
            var query = _repo.GetQueryable()
                .Include(r => r.User)
                .Include(r => r.Hotel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                if (Enum.TryParse<SupportRequestStatus>(status, out var statusEnum))
                    query = query.Where(r => r.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All")
                query = query.Where(r => r.SubmitterRole == role);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(r =>
                    r.Subject.ToLower().Contains(s) ||
                    r.Category.ToLower().Contains(s) ||
                    (r.GuestName != null && r.GuestName.ToLower().Contains(s)) ||
                    (r.GuestEmail != null && r.GuestEmail.ToLower().Contains(s)) ||
                    (r.User != null && r.User.Name.ToLower().Contains(s)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedSupportRequestResponseDto
            {
                TotalCount = total,
                Requests = items.Select(r =>
                {
                    var name = r.User?.Name ?? r.GuestName ?? string.Empty;
                    var email = r.User?.Email ?? r.GuestEmail ?? string.Empty;
                    return MapToDto(r, name, email, r.Hotel?.Name);
                })
            };
        }

        public async Task<SupportRequestResponseDto> RespondAsync(Guid requestId, RespondSupportRequestDto dto)
        {
            var request = await _repo.GetQueryable()
                .Include(r => r.User)
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.SupportRequestId == requestId)
                ?? throw new NotFoundException("Support request not found.");

            if (!Enum.TryParse<SupportRequestStatus>(dto.Status, out var newStatus)
                || newStatus == SupportRequestStatus.Open)
                newStatus = SupportRequestStatus.Resolved;

            request.Status = newStatus;
            request.RespondedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Response))
                request.AdminResponse = dto.Response;

            await _unitOfWork.SaveChangesAsync();

            var name = request.User?.Name ?? request.GuestName ?? string.Empty;
            var email = request.User?.Email ?? request.GuestEmail ?? string.Empty;
            return MapToDto(request, name, email, request.Hotel?.Name);
        }

        private static SupportRequestResponseDto MapToDto(
            SupportRequest r, string name, string email, string? hotelName) => new()
        {
            SupportRequestId = r.SupportRequestId,
            Subject = r.Subject,
            Message = r.Message,
            Category = r.Category,
            Status = r.Status.ToString(),
            AdminResponse = r.AdminResponse,
            SubmitterRole = r.SubmitterRole ?? "Public",
            SubmitterName = name,
            SubmitterEmail = email,
            ReservationCode = r.ReservationCode,
            HotelName = hotelName,
            CreatedAt = r.CreatedAt,
            RespondedAt = r.RespondedAt
        };
    }
}
