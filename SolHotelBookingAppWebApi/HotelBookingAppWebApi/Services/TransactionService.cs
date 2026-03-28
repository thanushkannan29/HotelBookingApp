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
        private readonly IRepository<Guid, ReservationRoom> _reservationRoomRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, ReservationRoom> reservationRoomRepo,
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IUnitOfWork unitOfWork)
        {
            _transactionRepo = transactionRepo;
            _reservationRepo = reservationRepo;
            _inventoryRepo = inventoryRepo;
            _reservationRoomRepo = reservationRoomRepo;
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _unitOfWork = unitOfWork;
        }

        // ── CREATE PAYMENT ────────────────────────────────────────────────────
        public async Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
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
                TransactionId   = Guid.NewGuid(),
                ReservationId   = reservation.ReservationId,
                Amount          = reservation.FinalAmount > 0 ? reservation.FinalAmount : reservation.TotalAmount,
                PaymentMethod   = dto.PaymentMethod,
                Status          = PaymentStatus.Success,
                TransactionDate = DateTime.UtcNow
            };

            // Promote reservation to Confirmed on successful payment
            reservation.Status = ReservationStatus.Confirmed;

            await _transactionRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();   // no transaction needed — simple insert + update

            return MapToDto(transaction);
        }

        // ── DIRECT REFUND (Guest only — within 30 minutes of payment) ─────────
        public async Task<TransactionResponseDto> DirectGuestRefundAsync(
            Guid transactionId, Guid userId, RefundRequestDto dto)
        {
            var transaction = await _transactionRepo.GetQueryable()
                .Include(t => t.Reservation)
                    .ThenInclude(r => r!.ReservationRooms)
                .Include(t => t.Reservation!.Transactions)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId)
                ?? throw new NotFoundException("Transaction not found.");

            if (transaction.Reservation!.UserId != userId)
                throw new UnAuthorizedException("You are not authorized to refund this transaction.");

            if (transaction.Status != PaymentStatus.Success)
                throw new PaymentException("Only successful transactions can be refunded.");

            var reservation = transaction.Reservation!;

            if (reservation.Status == ReservationStatus.Completed)
                throw new PaymentException("Completed reservations cannot be refunded.");

            if (reservation.Status == ReservationStatus.Cancelled)
                throw new PaymentException("This reservation is already cancelled.");

            var minutesSincePayment = (DateTime.UtcNow - transaction.TransactionDate).TotalMinutes;
            if (minutesSincePayment > 30)
                throw new PaymentException("Direct refund window has expired. Please submit a refund request instead.");

            transaction.Status = PaymentStatus.Refunded;
            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledDate = DateTime.UtcNow;
            reservation.CancellationReason = dto.Reason;

            var roomTypeId = reservation.ReservationRooms!.First().RoomTypeId;
            var roomCount = reservation.ReservationRooms.Count;
            var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
            var dates = Enumerable.Range(0, totalDays).Select(d => reservation.CheckInDate.AddDays(d)).ToList();

            var inventories = await _inventoryRepo.GetQueryable()
                .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                .ToListAsync();

            foreach (var inv in inventories)
                inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);

            await _unitOfWork.SaveChangesAsync();
            return MapToDto(transaction);
        }

        // ── GET ALL TRANSACTIONS (Role-based) ─────────────────────────────────
        // NOTE (Correction 10C): Admin branch has NO status filter — returns all statuses
        // (Success, Refunded, Failed) for the hotel. This is intentional.
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

                // No status filter — Admin sees ALL transaction statuses for their hotel
                query = query.Where(t => t.Reservation!.HotelId == hotelId);
            }
            // SuperAdmin → no filter — sees everything

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

        // ── PAYMENT INTENT (Correction 7D) ────────────────────────────────────
        // Guest calls this before paying — returns hotel UPI ID + payment reference.
        // This is purely informational; the actual UPI payment happens outside the app.
        public async Task<PaymentIntentDto> GetPaymentIntentAsync(Guid reservationId, Guid userId)
        {
            var reservation = await _reservationRepo.GetQueryable()
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.UserId == userId)
                ?? throw new NotFoundException("Reservation not found.");

            if (reservation.Status != ReservationStatus.Pending)
                throw new ValidationException("Payment intent is only available for pending reservations.");

            return new PaymentIntentDto
            {
                UpiId = reservation.Hotel?.UpiId,
                Amount = reservation.TotalAmount,
                PaymentRef = $"HTLPAY-{reservation.ReservationCode}",
                HotelName = reservation.Hotel?.Name ?? string.Empty
            };
        }

        // ── MARK TRANSACTION FAILED (Correction 7E) ───────────────────────────
        // Admin marks a payment as Failed if they did not receive it.
        // Resets reservation to Pending so the guest can attempt payment again.
        public async Task MarkTransactionFailedAsync(Guid transactionId, Guid adminUserId)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (admin.HotelId == null)
                throw new UnAuthorizedException("Unauthorized.");

            var transaction = await _transactionRepo.GetQueryable()
                .Include(t => t.Reservation)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId)
                ?? throw new NotFoundException("Transaction not found.");

            if (transaction.Reservation!.HotelId != admin.HotelId)
                throw new UnAuthorizedException("You are not authorized to manage this transaction.");

            if (transaction.Status != PaymentStatus.Success)
                throw new ValidationException("Only successful transactions can be marked as failed.");

            transaction.Status = PaymentStatus.Failed;
            transaction.Reservation.Status = ReservationStatus.Pending;

            // Restore inventory when transaction is marked failed
            var reservationRooms = await _reservationRoomRepo.GetQueryable()
                .Where(rr => rr.ReservationId == transaction.ReservationId)
                .ToListAsync();

            if (reservationRooms.Any())
            {
                var roomTypeId = reservationRooms.First().RoomTypeId;
                var reservation = transaction.Reservation!;
                var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
                var dates = Enumerable.Range(0, totalDays).Select(d => reservation.CheckInDate.AddDays(d)).ToList();
                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                    .ToListAsync();
                var roomCount = reservationRooms.Count;
                foreach (var inv in inventories)
                    inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);
            }

            await _unitOfWork.SaveChangesAsync();
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

        // ── RECORD FAILED PAYMENT (Razorpay failure) ──────────────────────────
        public async Task RecordFailedPaymentAsync(Guid reservationId, Guid userId)
        {
            var reservation = await _reservationRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.UserId == userId)
                ?? throw new NotFoundException("Reservation not found.");

            // Record a Failed transaction so there's an audit trail
            var transaction = new Transaction
            {
                TransactionId   = Guid.NewGuid(),
                ReservationId   = reservationId,
                Amount          = reservation.FinalAmount > 0 ? reservation.FinalAmount : reservation.TotalAmount,
                PaymentMethod   = PaymentMethod.UPI,
                Status          = PaymentStatus.Failed,
                TransactionDate = DateTime.UtcNow
            };

            await _transactionRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}