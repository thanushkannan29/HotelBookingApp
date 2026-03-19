using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RefundRequest;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class RefundRequestService : IRefundRequestService
    {
        private readonly IRepository<Guid, RefundRequest> _refundRepo;
        private readonly IRepository<Guid, Transaction> _transactionRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        public RefundRequestService(
            IRepository<Guid, RefundRequest> refundRepo,
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, User> userRepo,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork)
        {
            _refundRepo = refundRepo;
            _transactionRepo = transactionRepo;
            _reservationRepo = reservationRepo;
            _userRepo = userRepo;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        // ── CREATE REFUND REQUEST (called internally on cancellation) ─────────
        public async Task CreateRefundRequestAsync(Guid reservationId, Guid userId, string reason)
        {
            // Avoid duplicate pending requests for the same reservation
            var exists = await _refundRepo.GetQueryable()
                .AnyAsync(r => r.ReservationId == reservationId &&
                               r.Status == RefundRequestStatus.Pending);

            if (exists) return; // already has a pending request

            var refundRequest = new RefundRequest
            {
                RefundRequestId = Guid.NewGuid(),
                ReservationId = reservationId,
                UserId = userId,
                Reason = reason,
                Status = RefundRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _refundRepo.AddAsync(refundRequest);
            await _unitOfWork.SaveChangesAsync();
        }

        // ── ADMIN: APPROVE REFUND ─────────────────────────────────────────────
        public async Task<RefundRequestResponseDto> ApproveRefundAsync(
            Guid refundRequestId, Guid adminId, string adminResponse)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var refundRequest = await _refundRepo.GetQueryable()
                    .Include(r => r.Reservation)
                        .ThenInclude(res => res!.Transactions)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId)
                    ?? throw new NotFoundException("Refund request not found.");

                if (refundRequest.Status != RefundRequestStatus.Pending)
                    throw new ValidationException("Only pending refund requests can be approved.");

                // Validate admin owns this hotel
                var admin = await _userRepo.GetAsync(adminId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                if (admin.HotelId != refundRequest.Reservation!.HotelId)
                    throw new UnAuthorizedException("You are not authorized to manage this refund.");

                // Find the successful transaction for this reservation
                var transaction = refundRequest.Reservation.Transactions?
                    .FirstOrDefault(t => t.Status == PaymentStatus.Success)
                    ?? throw new PaymentException("No successful payment found for this reservation.");

                // Mark transaction as Refunded
                transaction.Status = PaymentStatus.Refunded;

                // Mark refund request as Approved
                refundRequest.Status = RefundRequestStatus.Approved;
                refundRequest.AdminResponse = adminResponse;
                refundRequest.ProcessedAt = DateTime.UtcNow;

                await _unitOfWork.CommitAsync();

                await _auditLogService.LogAsync(adminId, "RefundApproved", "RefundRequest",
                    refundRequest.RefundRequestId,
                    $"Reservation {refundRequest.Reservation.ReservationCode} refund approved. Response: {adminResponse}");

                return MapToDto(refundRequest, transaction.Amount);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── ADMIN: REJECT REFUND ──────────────────────────────────────────────
        public async Task<RefundRequestResponseDto> RejectRefundAsync(
            Guid refundRequestId, Guid adminId, string adminResponse)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var refundRequest = await _refundRepo.GetQueryable()
                    .Include(r => r.Reservation)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId)
                    ?? throw new NotFoundException("Refund request not found.");

                if (refundRequest.Status != RefundRequestStatus.Pending)
                    throw new ValidationException("Only pending refund requests can be rejected.");

                var admin = await _userRepo.GetAsync(adminId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                if (admin.HotelId != refundRequest.Reservation!.HotelId)
                    throw new UnAuthorizedException("You are not authorized to manage this refund.");

                refundRequest.Status = RefundRequestStatus.Rejected;
                refundRequest.AdminResponse = adminResponse;
                refundRequest.ProcessedAt = DateTime.UtcNow;

                await _unitOfWork.CommitAsync();

                await _auditLogService.LogAsync(adminId, "RefundRejected", "RefundRequest",
                    refundRequest.RefundRequestId,
                    $"Reservation {refundRequest.Reservation.ReservationCode} refund rejected. Response: {adminResponse}");

                // Find original transaction amount for DTO
                var amount = await _transactionRepo.GetQueryable()
                    .Where(t => t.ReservationId == refundRequest.ReservationId &&
                                t.Status == PaymentStatus.Success)
                    .Select(t => (decimal?)t.Amount)
                    .FirstOrDefaultAsync() ?? 0;

                return MapToDto(refundRequest, amount);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── ADMIN: LIST HOTEL REFUND REQUESTS ─────────────────────────────────
        public async Task<IEnumerable<RefundRequestResponseDto>> GetHotelRefundRequestsAsync(
            Guid adminUserId)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (admin.HotelId == null)
                throw new UnAuthorizedException("Unauthorized.");

            var requests = await _refundRepo.GetQueryable()
                .Include(r => r.Reservation)
                .Include(r => r.User)
                .Where(r => r.Reservation!.HotelId == admin.HotelId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return await EnrichWithAmount(requests);
        }

        // ── GUEST: LIST OWN REFUND REQUESTS ──────────────────────────────────
        public async Task<IEnumerable<RefundRequestResponseDto>> GetGuestRefundRequestsAsync(
            Guid userId)
        {
            var requests = await _refundRepo.GetQueryable()
                .Include(r => r.Reservation)
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return await EnrichWithAmount(requests);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────
        private async Task<IEnumerable<RefundRequestResponseDto>> EnrichWithAmount(
            List<RefundRequest> requests)
        {
            var reservationIds = requests.Select(r => r.ReservationId).Distinct().ToList();

            // Fetch all relevant transactions in one query
            var amounts = await _transactionRepo.GetQueryable()
                .Where(t => reservationIds.Contains(t.ReservationId) &&
                            (t.Status == PaymentStatus.Success || t.Status == PaymentStatus.Refunded))
                .GroupBy(t => t.ReservationId)
                .Select(g => new { ReservationId = g.Key, Amount = g.Max(t => t.Amount) })
                .ToDictionaryAsync(x => x.ReservationId, x => x.Amount);

            return requests.Select(r =>
            {
                amounts.TryGetValue(r.ReservationId, out var amount);
                return MapToDto(r, amount);
            });
        }

        private static RefundRequestResponseDto MapToDto(RefundRequest r, decimal amount) => new()
        {
            RefundRequestId = r.RefundRequestId,
            ReservationId = r.ReservationId,
            ReservationCode = r.Reservation?.ReservationCode ?? string.Empty,
            UserId = r.UserId,
            GuestName = r.User?.Name ?? string.Empty,
            Reason = r.Reason,
            Status = r.Status.ToString(),
            AdminResponse = r.AdminResponse,
            RefundAmount = amount,
            CreatedAt = r.CreatedAt,
            ProcessedAt = r.ProcessedAt
        };
    }
}
