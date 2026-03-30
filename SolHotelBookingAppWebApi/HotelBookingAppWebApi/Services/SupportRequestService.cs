using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Handles support request creation and resolution for all roles
    /// (Public, Guest, Admin, SuperAdmin).
    /// </summary>
    public class SupportRequestService : ISupportRequestService
    {
        private readonly IRepository<Guid, SupportRequest> _supportRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IUnitOfWork _unitOfWork;

        public SupportRequestService(
            IRepository<Guid, SupportRequest> supportRepo,
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IUnitOfWork unitOfWork)
        {
            _supportRepo = supportRepo;
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<SupportRequestResponseDto> CreatePublicRequestAsync(PublicSupportRequestDto dto)
        {
            var request = BuildPublicRequest(dto);
            await _supportRepo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(request, dto.Name, dto.Email, null);
        }

        public async Task<SupportRequestResponseDto> CreateGuestRequestAsync(
            Guid userId, GuestSupportRequestDto dto)
        {
            var user = await GetUserOrThrowAsync(userId);
            var hotelName = dto.HotelId.HasValue
                ? (await _hotelRepo.GetAsync(dto.HotelId.Value))?.Name
                : null;

            var request = BuildGuestRequest(userId, dto);
            await _supportRepo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(request, user.Name, user.Email, hotelName);
        }

        public async Task<SupportRequestResponseDto> CreateAdminRequestAsync(
            Guid adminUserId, AdminSupportRequestDto dto)
        {
            var user = await GetUserOrThrowAsync(adminUserId);
            var request = BuildAdminRequest(adminUserId, dto);
            await _supportRepo.AddAsync(request);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(request, user.Name, user.Email, null);
        }

        public async Task<PagedSupportRequestResponseDto> GetGuestRequestsAsync(
            Guid userId, int page, int pageSize)
        {
            var user = await GetUserOrThrowAsync(userId);
            var query = _supportRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt);

            return await BuildPagedResponseAsync(query, page, pageSize,
                r => MapToDto(r, user.Name, user.Email, r.Hotel?.Name));
        }

        public async Task<PagedSupportRequestResponseDto> GetAdminRequestsAsync(
            Guid adminUserId, int page, int pageSize)
        {
            var user = await GetUserOrThrowAsync(adminUserId);
            var query = _supportRepo.GetQueryable()
                .Where(r => r.UserId == adminUserId)
                .OrderByDescending(r => r.CreatedAt);

            return await BuildPagedResponseAsync(query, page, pageSize,
                r => MapToDto(r, user.Name, user.Email, null));
        }

        public async Task<PagedSupportRequestResponseDto> GetAllRequestsAsync(
            string? status, string? role, string? search, int page, int pageSize)
        {
            var query = BuildSuperAdminQuery(status, role, search);
            return await BuildPagedResponseAsync(query, page, pageSize, r =>
            {
                var name = r.User?.Name ?? r.GuestName ?? string.Empty;
                var email = r.User?.Email ?? r.GuestEmail ?? string.Empty;
                return MapToDto(r, name, email, r.Hotel?.Name);
            });
        }

        public async Task<SupportRequestResponseDto> RespondAsync(
            Guid requestId, RespondSupportRequestDto dto)
        {
            var request = await _supportRepo.GetQueryable()
                .Include(r => r.User)
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.SupportRequestId == requestId)
                ?? throw new NotFoundException("Support request not found.");

            ApplyResponse(request, dto);
            await _unitOfWork.SaveChangesAsync();

            var name = request.User?.Name ?? request.GuestName ?? string.Empty;
            var email = request.User?.Email ?? request.GuestEmail ?? string.Empty;
            return MapToDto(request, name, email, request.Hotel?.Name);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<User> GetUserOrThrowAsync(Guid userId)
            => await _userRepo.GetAsync(userId) ?? throw new UnAuthorizedException("Unauthorized.");

        private static SupportRequest BuildPublicRequest(PublicSupportRequestDto dto) => new()
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

        private static SupportRequest BuildGuestRequest(Guid userId, GuestSupportRequestDto dto) => new()
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

        private static SupportRequest BuildAdminRequest(Guid adminUserId, AdminSupportRequestDto dto) => new()
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

        private IQueryable<SupportRequest> BuildSuperAdminQuery(
            string? status, string? role, string? search)
        {
            var query = _supportRepo.GetQueryable()
                .Include(r => r.User)
                .Include(r => r.Hotel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All" &&
                Enum.TryParse<SupportRequestStatus>(status, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(role) && role != "All")
                query = query.Where(r => r.SubmitterRole == role);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(r =>
                    r.Subject.ToLower().Contains(term) ||
                    r.Category.ToLower().Contains(term) ||
                    (r.GuestName != null && r.GuestName.ToLower().Contains(term)) ||
                    (r.GuestEmail != null && r.GuestEmail.ToLower().Contains(term)) ||
                    (r.User != null && r.User.Name.ToLower().Contains(term)));
            }

            return query.OrderByDescending(r => r.CreatedAt);
        }

        private static async Task<PagedSupportRequestResponseDto> BuildPagedResponseAsync(
            IQueryable<SupportRequest> query, int page, int pageSize,
            Func<SupportRequest, SupportRequestResponseDto> mapper)
        {
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedSupportRequestResponseDto
            {
                TotalCount = total,
                Requests = items.Select(mapper)
            };
        }

        private static void ApplyResponse(SupportRequest request, RespondSupportRequestDto dto)
        {
            var newStatus = Enum.TryParse<SupportRequestStatus>(dto.Status, out var parsed) && parsed != SupportRequestStatus.Open
                ? parsed
                : SupportRequestStatus.Resolved;

            request.Status = newStatus;
            request.RespondedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Response))
                request.AdminResponse = dto.Response;
        }

        private static SupportRequestResponseDto MapToDto(
            SupportRequest request, string name, string email, string? hotelName) => new()
        {
            SupportRequestId = request.SupportRequestId,
            Subject = request.Subject,
            Message = request.Message,
            Category = request.Category,
            Status = request.Status.ToString(),
            AdminResponse = request.AdminResponse,
            SubmitterRole = request.SubmitterRole ?? "Public",
            SubmitterName = name,
            SubmitterEmail = email,
            ReservationCode = request.ReservationCode,
            HotelName = hotelName,
            CreatedAt = request.CreatedAt,
            RespondedAt = request.RespondedAt
        };
    }
}
