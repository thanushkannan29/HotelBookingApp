using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly HotelBookingContext _context;

        public TransactionService(HotelBookingContext context)
        {
            _context = context;
        }

        // ============================================
        // CREATE PAYMENT
        // ============================================
        public async Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Transactions)
                .FirstOrDefaultAsync(r => r.ReservationId == dto.ReservationId);

            if (reservation == null)
                throw new NotFoundException("Reservation not found.");

            if (reservation.Status == ReservationStatus.Cancelled)
                throw new PaymentException("Cannot pay for cancelled reservation.");

            if (reservation.Transactions.Any(t => t.Status == PaymentStatus.Success))
                throw new PaymentException("Reservation already paid.");

            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                ReservationId = reservation.ReservationId,
                Amount = reservation.TotalAmount,
                PaymentMethod = dto.PaymentMethod,
                Status = PaymentStatus.Success,
                TransactionDate = DateTime.UtcNow
            };

            reservation.Status = ReservationStatus.Confirmed;

            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            return MapToDto(transaction);
        }

        

        // ============================================
        // REFUND
        // ============================================
        public async Task<TransactionResponseDto> RefundAsync(
            Guid transactionId,
            RefundRequestDto dto)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Reservation)
                        .ThenInclude(r => r.ReservationRooms)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

                if (transaction == null)
                    throw new NotFoundException("Transaction not found.");

                if (transaction.Status != PaymentStatus.Success)
                    throw new PaymentException("Only successful payments can be refunded.");

                transaction.Status = PaymentStatus.Refunded;

                var reservation = transaction.Reservation;

                if (reservation != null)
                {
                    reservation.Status = ReservationStatus.Cancelled;
                    reservation.CancelledDate = DateTime.UtcNow;
                    reservation.CancellationReason = dto.Reason;

                    var room = reservation.ReservationRooms.First();

                    var totalDays = reservation.CheckOutDate.DayNumber -
                                    reservation.CheckInDate.DayNumber;

                    var dates = Enumerable.Range(0, totalDays)
                        .Select(offset => reservation.CheckInDate.AddDays(offset))
                        .ToList();

                    var inventories = await _context.RoomTypeInventories
                        .Where(i =>
                            i.RoomTypeId == room.RoomTypeId &&
                            dates.Contains(i.Date))
                        .ToListAsync();

                    foreach (var inventory in inventories)
                        inventory.ReservedInventory -= room.NumberOfRooms;
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return MapToDto(transaction);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        // ============================================
        // PAGINATION
        // ============================================
        public async Task<PagedTransactionResponseDto> GetAllTransactionsAsync(
            int page,
            int pageSize)
        {
            var query = _context.Transactions.AsQueryable();

            var total = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionResponseDto
                {
                    TransactionId = t.TransactionId,
                    ReservationId = t.ReservationId,
                    Amount = t.Amount,
                    PaymentMethod = t.PaymentMethod,
                    Status = t.Status,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            return new PagedTransactionResponseDto
            {
                TotalCount = total,
                Transactions = transactions
            };

        }
        private static TransactionResponseDto MapToDto(Transaction t)
        {
            return new TransactionResponseDto
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
}
