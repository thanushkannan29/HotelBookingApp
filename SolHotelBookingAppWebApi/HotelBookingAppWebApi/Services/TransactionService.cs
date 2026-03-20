using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IRepository<Guid, Transaction> _transactionRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, User> userRepo,
            IUnitOfWork unitOfWork)
        {
            _transactionRepo = transactionRepo;
            _reservationRepo = reservationRepo;
            _inventoryRepo = inventoryRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        // ── CREATE PAYMENT ────────────────────────────────────────────────────
        public async Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var reservation = await _reservationRepo.GetQueryable()
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.ReservationId == dto.ReservationId)
                    ?? throw new NotFoundException("Reservation not found.");

                if (reservation.Status == ReservationStatus.Cancelled)
                    throw new PaymentException("Cannot pay for a cancelled reservation.");

                if (reservation.Status == ReservationStatus.Completed)
                    throw new PaymentException("Cannot pay for a completed reservation.");

                if (reservation.ExpiryTime.HasValue && reservation.ExpiryTime < DateTime.UtcNow
                    && reservation.Status == ReservationStatus.Pending)
                    throw new PaymentException("Reservation has expired. Please create a new booking.");

                if (reservation.Transactions!.Any(t => t.Status == PaymentStatus.Success))
                    throw new PaymentException("This reservation has already been paid.");

                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    ReservationId = reservation.ReservationId,
                    Amount = reservation.TotalAmount,
                    PaymentMethod = dto.PaymentMethod,
                    Status = PaymentStatus.Success,
                    TransactionDate = DateTime.UtcNow
                };

                // Promote reservation to Confirmed on successful payment
                reservation.Status = ReservationStatus.Confirmed;

                await _transactionRepo.AddAsync(transaction);
                await _unitOfWork.CommitAsync();

                return MapToDto(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── DIRECT REFUND (Guest only — within 30 minutes of payment) ─────────
        // The UI hides this button after 30 min. The backend also enforces the window.
        // Admin refunds must go through the RefundRequest approve/reject flow.
        public async Task<TransactionResponseDto> DirectGuestRefundAsync(
            Guid transactionId, Guid userId, RefundRequestDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var transaction = await _transactionRepo.GetQueryable()
                    .Include(t => t.Reservation)
                        .ThenInclude(r => r!.ReservationRooms)
                    .Include(t => t.Reservation!.Transactions)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId)
                    ?? throw new NotFoundException("Transaction not found.");

                // Ensure the reservation belongs to this guest
                if (transaction.Reservation!.UserId != userId)
                    throw new UnAuthorizedException("You are not authorized to refund this transaction.");

                if (transaction.Status != PaymentStatus.Success)
                    throw new PaymentException("Only successful transactions can be refunded.");

                var reservation = transaction.Reservation!;

                if (reservation.Status == ReservationStatus.Completed)
                    throw new PaymentException("Completed reservations cannot be refunded.");

                if (reservation.Status == ReservationStatus.Cancelled)
                    throw new PaymentException("This reservation is already cancelled.");

                // ── 30-minute guest window enforcement ────────────────────────
                var minutesSincePayment = (DateTime.UtcNow - transaction.TransactionDate).TotalMinutes;
                if (minutesSincePayment > 30)
                    throw new PaymentException(
                        "Direct refund window has expired. Please submit a refund request instead.");

                // Mark transaction refunded
                transaction.Status = PaymentStatus.Refunded;

                // Cancel reservation
                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledDate = DateTime.UtcNow;
                reservation.CancellationReason = dto.Reason;

                // Restore inventory
                var roomTypeId = reservation.ReservationRooms!.First().RoomTypeId;
                var roomCount = reservation.ReservationRooms.Count;
                var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

                var dates = Enumerable.Range(0, totalDays)
                    .Select(d => reservation.CheckInDate.AddDays(d))
                    .ToList();

                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                foreach (var inv in inventories)
                    inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);

                await _unitOfWork.CommitAsync();
                return MapToDto(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── GET ALL TRANSACTIONS (Role-based) ─────────────────────────────────
        public async Task<PagedTransactionResponseDto> GetAllTransactionsAsync(
            Guid userId, string role, int page, int pageSize)
        {
            var query = _transactionRepo.GetQueryable().AsQueryable();

            if (role == "Guest")
            {
                query = query.Where(t => t.Reservation!.UserId == userId);
            }
            else if (role == "Admin")
            {
                var hotelId = await _userRepo.GetQueryable()
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HotelId)
                    .FirstOrDefaultAsync();

                if (hotelId == null)
                    throw new NotFoundException("Admin hotel not found.");

                query = query.Where(t => t.Reservation!.HotelId == hotelId);
            }
            // SuperAdmin → no filter

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedTransactionResponseDto
            {
                TotalCount = total,
                Transactions = data.Select(MapToDto)
            };
        }

        private static TransactionResponseDto MapToDto(Transaction t) => new()
        {
            TransactionId = t.TransactionId,
            ReservationId = t.ReservationId,
            Amount = t.Amount,
            PaymentMethod = t.PaymentMethod,
            Status = t.Status,
            TransactionDate = t.TransactionDate
        };
    }
}