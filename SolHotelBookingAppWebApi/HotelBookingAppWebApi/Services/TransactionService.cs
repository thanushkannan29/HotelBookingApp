using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class TransactionService(
        IRepository<Guid, Transaction> transactionRepo,
        IRepository<Guid, Reservation> reservationRepo,
        IRepository<Guid, RoomTypeInventory> inventoryRepo,
        IRepository<Guid, ReservationRoom> reservationRoomRepo,
        IRepository<Guid, User> userRepo,
        IRepository<Guid, Hotel> hotelRepo,
        IRepository<Guid, Wallet> walletRepo,
        IRepository<Guid, WalletTransaction> walletTxRepo,
        IRepository<Guid, SuperAdminRevenue> revenueRepo,
        IUnitOfWork unitOfWork) : ITransactionService
    {
        private readonly IRepository<Guid, Transaction> _transactionRepo = transactionRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo = reservationRepo;
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo = inventoryRepo;
        private readonly IRepository<Guid, ReservationRoom> _reservationRoomRepo = reservationRoomRepo;
        private readonly IRepository<Guid, User> _userRepo = userRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo = hotelRepo;
        private readonly IRepository<Guid, Wallet> _walletRepo = walletRepo;
        private readonly IRepository<Guid, WalletTransaction> _walletTxRepo = walletTxRepo;
        private readonly IRepository<Guid, SuperAdminRevenue> _revenueRepo = revenueRepo;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

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

            var reservationRooms = await _reservationRoomRepo.GetQueryable()
                .Where(rr => rr.ReservationId == reservation.ReservationId)
                .ToListAsync();

            if (reservationRooms.Count > 0)
            {
                var roomTypeId = reservationRooms.First().RoomTypeId;
                var roomCount = reservationRooms.Count;
                var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;
                var dates = Enumerable.Range(0, totalDays).Select(d => reservation.CheckInDate.AddDays(d)).ToList();

                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                foreach (var inv in inventories)
                    inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);
            }

            await _unitOfWork.SaveChangesAsync();
            return MapToDto(transaction);
        }

        // ── GET ALL TRANSACTIONS (Role-based) ─────────────────────────────────
        // NOTE (Correction 10C): Admin branch has NO status filter — returns all statuses
        // (Success, Refunded, Failed) for the hotel. This is intentional.
        public async Task<PagedTransactionResponseDto> GetAllTransactionsAsync(
            Guid userId, string role, int page, int pageSize,
            string? sortField = null, string? sortDir = null)
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
                    .FirstOrDefaultAsync()
                    ?? throw new NotFoundException("Admin hotel not found.");

                // No status filter — Admin sees ALL transaction statuses for their hotel
                query = query.Where(t => t.Reservation!.HotelId == hotelId);
            }
            // SuperAdmin → no filter — sees everything

            var total = await query.CountAsync();

            var data = await query
                .Include(t => t.Reservation)
                    .ThenInclude(r => r!.Hotel)
                .Include(t => t.Reservation)
                    .ThenInclude(r => r!.User)
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var txList = data.Select(MapToDto).ToList();

            // ── GUEST: append wallet refund credits ───────────────────────────
            if (role == "Guest")
            {
                var wallet = await _walletRepo.GetQueryable()
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (wallet is not null)
                {
                    var refundEntries = await _walletTxRepo.GetQueryable()
                        .Where(wt => wt.WalletId == wallet.WalletId &&
                                     wt.Type == "Credit" &&
                                     wt.Description.Contains("Refund"))
                        .OrderByDescending(wt => wt.CreatedAt)
                        .ToListAsync();

                    txList.AddRange(refundEntries.Select(wt => new TransactionResponseDto
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
                    }));

                    txList = [.. txList.OrderByDescending(t => t.TransactionDate)];
                    total += refundEntries.Count;
                }
            }

            // ── ADMIN: append auto-refund sent to guests + commission to superadmin ──
            if (role == "Admin")
            {
                var adminHotelId = await _userRepo.GetQueryable()
                    .Where(u => u.UserId == userId)
                    .Select(u => u.HotelId)
                    .FirstOrDefaultAsync();

                if (adminHotelId is not null)
                {
                    var commissions = await _revenueRepo.GetQueryable()
                        .Include(r => r.Reservation)
                        .Where(r => r.HotelId == adminHotelId)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToListAsync();

                    txList.AddRange(commissions.Select(c => new TransactionResponseDto
                    {
                        TransactionId = c.SuperAdminRevenueId,
                        ReservationId = c.ReservationId,
                        ReservationCode = c.Reservation?.ReservationCode ?? string.Empty,
                        HotelName = string.Empty,
                        GuestName = string.Empty,
                        Amount = c.CommissionAmount,
                        PaymentMethod = PaymentMethod.UPI,
                        Status = PaymentStatus.Success,
                        TransactionDate = c.CreatedAt,
                        TransactionType = "CommissionSent",
                        Description = $"2% commission sent to SuperAdmin for reservation {c.Reservation?.ReservationCode}"
                    }));

                    // Auto-refunds sent to guests (wallet credits with "Refund" in description for this hotel's reservations)
                    var hotelReservationIds = await _reservationRepo.GetQueryable()
                        .Where(r => r.HotelId == adminHotelId)
                        .Select(r => r.ReservationId)
                        .ToListAsync();

                    var guestUserIds = await _reservationRepo.GetQueryable()
                        .Where(r => r.HotelId == adminHotelId)
                        .Select(r => r.UserId)
                        .Distinct()
                        .ToListAsync();

                    var guestWalletIds = await _walletRepo.GetQueryable()
                        .Where(w => guestUserIds.Contains(w.UserId))
                        .Select(w => new { w.WalletId, w.UserId })
                        .ToListAsync();

                    var walletIdList = guestWalletIds.Select(w => w.WalletId).ToList();

                    var autoRefunds = await _walletTxRepo.GetQueryable()
                        .Where(wt => walletIdList.Contains(wt.WalletId) &&
                                     wt.Type == "Credit" &&
                                     wt.Description.Contains("Refund"))
                        .OrderByDescending(wt => wt.CreatedAt)
                        .ToListAsync();

                    var walletUserMap = guestWalletIds.ToDictionary(w => w.WalletId, w => w.UserId);
                    var userNames = await _userRepo.GetQueryable()
                        .Where(u => guestUserIds.Contains(u.UserId))
                        .Select(u => new { u.UserId, u.Name })
                        .ToListAsync();
                    var userNameMap = userNames.ToDictionary(u => u.UserId, u => u.Name);

                    foreach (var wt in autoRefunds)
                    {
                        var guestUserId = walletUserMap.GetValueOrDefault(wt.WalletId);
                        var guestName = guestUserId != Guid.Empty ? userNameMap.GetValueOrDefault(guestUserId) ?? string.Empty : string.Empty;

                        txList.Add(new TransactionResponseDto
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
                        });
                    }

                    txList = txList.OrderByDescending(t => t.TransactionDate).ToList();
                    total += commissions.Count + autoRefunds.Count;
                }
            }

            return new PagedTransactionResponseDto
            {
                TotalCount = total,
                Transactions = txList
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

            if (admin.HotelId is null)
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

            var reservationRooms = await _reservationRoomRepo.GetQueryable()
                .Where(rr => rr.ReservationId == transaction.ReservationId)
                .ToListAsync();

            if (reservationRooms.Count > 0)
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