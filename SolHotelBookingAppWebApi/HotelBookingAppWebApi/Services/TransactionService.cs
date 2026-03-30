using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages payment creation, refunds, and transaction history.
    /// Role-based transaction views are composed from sub-queries to keep each method focused.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly IRepository<Guid, Transaction> _transactionRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo;
        private readonly IRepository<Guid, ReservationRoom> _reservationRoomRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, Wallet> _walletRepo;
        private readonly IRepository<Guid, WalletTransaction> _walletTransactionRepo;
        private readonly IRepository<Guid, SuperAdminRevenue> _revenueRepo;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, ReservationRoom> reservationRoomRepo,
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, Wallet> walletRepo,
            IRepository<Guid, WalletTransaction> walletTransactionRepo,
            IRepository<Guid, SuperAdminRevenue> revenueRepo,
            IUnitOfWork unitOfWork)
        {
            _transactionRepo = transactionRepo;
            _reservationRepo = reservationRepo;
            _inventoryRepo = inventoryRepo;
            _reservationRoomRepo = reservationRoomRepo;
            _userRepo = userRepo;
            _hotelRepo = hotelRepo;
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _revenueRepo = revenueRepo;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
        {
            var reservation = await GetReservationForPaymentAsync(dto.ReservationId);
            ValidateReservationForPayment(reservation);

            var transaction = BuildSuccessTransaction(reservation, dto.PaymentMethod);
            reservation.Status = ReservationStatus.Confirmed;

            await _transactionRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(transaction);
        }

        public async Task<TransactionResponseDto> DirectGuestRefundAsync(
            Guid transactionId, Guid userId, RefundRequestDto dto)
        {
            var transaction = await GetTransactionWithReservationAsync(transactionId);
            ValidateDirectRefund(transaction, userId);

            var reservation = transaction.Reservation!;
            transaction.Status = PaymentStatus.Refunded;
            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledDate = DateTime.UtcNow;
            reservation.CancellationReason = dto.Reason;

            await RestoreInventoryForReservationAsync(reservation);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(transaction);
        }

        public async Task<PagedTransactionResponseDto> GetAllTransactionsAsync(
            Guid userId, string role, int page, int pageSize,
            string? sortField = null, string? sortDir = null)
        {
            var transactions = await FetchBaseTransactionsAsync(userId, role);
            var combined = transactions.Select(MapToDto).ToList();

            if (role == "Guest") await AppendGuestWalletRefundsAsync(userId, combined);
            if (role == "Admin") await AppendAdminExtrasAsync(userId, combined);

            var sorted = combined.OrderByDescending(t => t.TransactionDate).ToList();
            var paged = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedTransactionResponseDto { TotalCount = sorted.Count, Transactions = paged };
        }

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

        public async Task MarkTransactionFailedAsync(Guid transactionId, Guid adminUserId)
        {
            var admin = await GetAdminWithHotelAsync(adminUserId);
            var transaction = await GetTransactionWithReservationAsync(transactionId);

            EnsureTransactionBelongsToAdminHotel(transaction, admin.HotelId!.Value);
            if (transaction.Status != PaymentStatus.Success)
                throw new ValidationException("Only successful transactions can be marked as failed.");

            transaction.Status = PaymentStatus.Failed;
            transaction.Reservation!.Status = ReservationStatus.Pending;

            await RestoreInventoryForReservationAsync(transaction.Reservation);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RecordFailedPaymentAsync(Guid reservationId, Guid userId)
        {
            var reservation = await _reservationRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.UserId == userId)
                ?? throw new NotFoundException("Reservation not found.");

            var transaction = BuildFailedTransaction(reservation);
            await _transactionRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
        }

        // ── PRIVATE: FETCH HELPERS ────────────────────────────────────────────

        private async Task<Reservation> GetReservationForPaymentAsync(Guid reservationId)
            => await _reservationRepo.GetQueryable()
                .Include(r => r.Transactions)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId)
                ?? throw new NotFoundException("Reservation not found.");

        private async Task<Transaction> GetTransactionWithReservationAsync(Guid transactionId)
            => await _transactionRepo.GetQueryable()
                .Include(t => t.Reservation).ThenInclude(r => r!.ReservationRooms)
                .Include(t => t.Reservation!.Transactions)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId)
                ?? throw new NotFoundException("Transaction not found.");

        private async Task<User> GetAdminWithHotelAsync(Guid adminUserId)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.HotelId is null) throw new UnAuthorizedException("Unauthorized.");
            return admin;
        }

        private async Task<List<Transaction>> FetchBaseTransactionsAsync(Guid userId, string role)
        {
            var query = _transactionRepo.GetQueryable()
                .Include(t => t.Reservation).ThenInclude(r => r!.Hotel)
                .Include(t => t.Reservation).ThenInclude(r => r!.User)
                .AsQueryable();

            if (role == "Guest")
            {
                query = query.Where(t => t.Reservation!.UserId == userId);
            }
            else if (role == "Admin")
            {
                var hotelId = await GetAdminHotelIdAsync(userId);
                query = query.Where(t => t.Reservation!.HotelId == hotelId);
            }
            // SuperAdmin — no filter, sees everything

            return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
        }

        private async Task<Guid?> GetAdminHotelIdAsync(Guid adminUserId)
        {
            var hotelId = await _userRepo.GetQueryable()
                .Where(u => u.UserId == adminUserId)
                .Select(u => u.HotelId)
                .FirstOrDefaultAsync();

            if (hotelId is null) throw new NotFoundException("Admin hotel not found.");
            return hotelId;
        }

        // ── PRIVATE: GUEST WALLET REFUNDS ─────────────────────────────────────

        private async Task AppendGuestWalletRefundsAsync(
            Guid userId, List<TransactionResponseDto> combined)
        {
            var wallet = await _walletRepo.GetQueryable()
                .FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet is null) return;

            var refunds = await FetchWalletRefundsAsync(wallet.WalletId);
            combined.AddRange(refunds.Select(wt => MapWalletRefundToDto(wt)));
        }

        private async Task<List<WalletTransaction>> FetchWalletRefundsAsync(Guid walletId)
            => await _walletTransactionRepo.GetQueryable()
                .Where(wt => wt.WalletId == walletId &&
                             wt.Type == "Credit" &&
                             wt.Description.Contains("Refund"))
                .OrderByDescending(wt => wt.CreatedAt)
                .ToListAsync();

        // ── PRIVATE: ADMIN EXTRAS (commissions + auto-refunds) ────────────────

        private async Task AppendAdminExtrasAsync(
            Guid adminUserId, List<TransactionResponseDto> combined)
        {
            var hotelId = await GetAdminHotelIdAsync(adminUserId);
            if (hotelId is null) return;

            await AppendCommissionsAsync(hotelId.Value, combined);
            await AppendAutoRefundsAsync(hotelId.Value, combined);
        }

        private async Task AppendCommissionsAsync(
            Guid hotelId, List<TransactionResponseDto> combined)
        {
            var commissions = await _revenueRepo.GetQueryable()
                .Include(r => r.Reservation)
                .Where(r => r.HotelId == hotelId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            combined.AddRange(commissions.Select(MapCommissionToDto));
        }

        private async Task AppendAutoRefundsAsync(
            Guid hotelId, List<TransactionResponseDto> combined)
        {
            var (walletIds, userNameMap) = await LoadGuestWalletDataAsync(hotelId);
            if (!walletIds.Any()) return;

            var autoRefunds = await _walletTransactionRepo.GetQueryable()
                .Where(wt => walletIds.Keys.Contains(wt.WalletId) &&
                             wt.Type == "Credit" &&
                             wt.Description.Contains("Refund"))
                .OrderByDescending(wt => wt.CreatedAt)
                .ToListAsync();

            combined.AddRange(autoRefunds.Select(wt =>
            {
                var guestUserId = walletIds.GetValueOrDefault(wt.WalletId);
                var guestName = guestUserId != Guid.Empty
                    ? userNameMap.GetValueOrDefault(guestUserId) ?? string.Empty
                    : string.Empty;
                return MapAutoRefundToDto(wt, guestName);
            }));
        }

        private async Task<(Dictionary<Guid, Guid> walletIds, Dictionary<Guid, string> userNames)>
            LoadGuestWalletDataAsync(Guid hotelId)
        {
            var guestUserIds = await _reservationRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId)
                .Select(r => r.UserId)
                .Distinct()
                .ToListAsync();

            var walletEntries = await _walletRepo.GetQueryable()
                .Where(w => guestUserIds.Contains(w.UserId))
                .Select(w => new { w.WalletId, w.UserId })
                .ToListAsync();

            var userNames = await _userRepo.GetQueryable()
                .Where(u => guestUserIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Name })
                .ToDictionaryAsync(u => u.UserId, u => u.Name);

            var walletIds = walletEntries.ToDictionary(w => w.WalletId, w => w.UserId);
            return (walletIds, userNames);
        }

        // ── PRIVATE: VALIDATION ───────────────────────────────────────────────

        private static void ValidateReservationForPayment(Reservation reservation)
        {
            if (reservation.Status == ReservationStatus.Cancelled)
                throw new PaymentException("Cannot pay for a cancelled reservation.");
            if (reservation.Status == ReservationStatus.Completed)
                throw new PaymentException("Cannot pay for a completed reservation.");
            if (reservation.ExpiryTime.HasValue &&
                reservation.ExpiryTime < DateTime.UtcNow &&
                reservation.Status == ReservationStatus.Pending)
                throw new PaymentException("Reservation has expired. Please create a new booking.");
            if (reservation.Transactions!.Any(t => t.Status == PaymentStatus.Success))
                throw new PaymentException("This reservation has already been paid.");
        }

        private static void ValidateDirectRefund(Transaction transaction, Guid userId)
        {
            if (transaction.Reservation!.UserId != userId)
                throw new UnAuthorizedException("You are not authorized to refund this transaction.");
            if (transaction.Status != PaymentStatus.Success)
                throw new PaymentException("Only successful transactions can be refunded.");
            if (transaction.Reservation.Status == ReservationStatus.Completed)
                throw new PaymentException("Completed reservations cannot be refunded.");
            if (transaction.Reservation.Status == ReservationStatus.Cancelled)
                throw new PaymentException("This reservation is already cancelled.");

            var minutesSincePayment = (DateTime.UtcNow - transaction.TransactionDate).TotalMinutes;
            if (minutesSincePayment > 30)
                throw new PaymentException("Direct refund window has expired. Please submit a refund request instead.");
        }

        private static void EnsureTransactionBelongsToAdminHotel(Transaction transaction, Guid hotelId)
        {
            if (transaction.Reservation!.HotelId != hotelId)
                throw new UnAuthorizedException("You are not authorized to manage this transaction.");
        }

        // ── PRIVATE: INVENTORY RESTORE ────────────────────────────────────────

        private async Task RestoreInventoryForReservationAsync(Reservation reservation)
        {
            var rooms = await _reservationRoomRepo.GetQueryable()
                .Where(rr => rr.ReservationId == reservation.ReservationId)
                .ToListAsync();

            if (!rooms.Any()) return;

            var roomTypeId = rooms.First().RoomTypeId;
            var roomCount = rooms.Count;
            var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
            var dates = Enumerable.Range(0, totalDays)
                .Select(d => reservation.CheckInDate.AddDays(d))
                .ToList();

            var inventories = await _inventoryRepo.GetQueryable()
                .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                .ToListAsync();

            foreach (var inv in inventories)
                inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);
        }

        // ── PRIVATE: BUILDERS ─────────────────────────────────────────────────

        private static Transaction BuildSuccessTransaction(
            Reservation reservation, PaymentMethod method) => new()
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = reservation.FinalAmount > 0 ? reservation.FinalAmount : reservation.TotalAmount,
            PaymentMethod = method,
            Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow
        };

        private static Transaction BuildFailedTransaction(Reservation reservation) => new()
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = reservation.FinalAmount > 0 ? reservation.FinalAmount : reservation.TotalAmount,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Failed,
            TransactionDate = DateTime.UtcNow
        };

        // ── PRIVATE: MAPPERS ──────────────────────────────────────────────────

        private static TransactionResponseDto MapToDto(Transaction transaction) => new()
        {
            TransactionId = transaction.TransactionId,
            ReservationId = transaction.ReservationId,
            ReservationCode = transaction.Reservation?.ReservationCode ?? string.Empty,
            HotelName = transaction.Reservation?.Hotel?.Name ?? string.Empty,
            GuestName = transaction.Reservation?.User?.Name ?? string.Empty,
            Amount = transaction.Amount,
            PaymentMethod = transaction.PaymentMethod,
            Status = transaction.Status,
            TransactionDate = transaction.TransactionDate,
            TransactionType = "Payment"
        };

        private static TransactionResponseDto MapWalletRefundToDto(WalletTransaction wt) => new()
        {
            TransactionId = wt.WalletTransactionId,
            ReservationId = Guid.Empty,
            ReservationCode = string.Empty,
            HotelName = string.Empty,
            GuestName = string.Empty,
            Amount = wt.Amount,
            PaymentMethod = PaymentMethod.Wallet,
            Status = PaymentStatus.Refunded,
            TransactionDate = wt.CreatedAt,
            TransactionType = "WalletRefund",
            Description = wt.Description
        };

        private static TransactionResponseDto MapCommissionToDto(SuperAdminRevenue commission) => new()
        {
            TransactionId = commission.SuperAdminRevenueId,
            ReservationId = commission.ReservationId,
            ReservationCode = commission.Reservation?.ReservationCode ?? string.Empty,
            HotelName = string.Empty,
            GuestName = string.Empty,
            Amount = commission.CommissionAmount,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Success,
            TransactionDate = commission.CreatedAt,
            TransactionType = "CommissionSent",
            Description = $"2% commission sent to SuperAdmin for reservation {commission.Reservation?.ReservationCode}"
        };

        private static TransactionResponseDto MapAutoRefundToDto(
            WalletTransaction wt, string guestName) => new()
        {
            TransactionId = wt.WalletTransactionId,
            ReservationId = Guid.Empty,
            ReservationCode = string.Empty,
            HotelName = string.Empty,
            GuestName = guestName,
            Amount = wt.Amount,
            PaymentMethod = PaymentMethod.Wallet,
            Status = PaymentStatus.Refunded,
            TransactionDate = wt.CreatedAt,
            TransactionType = "AutoRefund",
            Description = wt.Description
        };
    }
}
