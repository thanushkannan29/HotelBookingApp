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
                .FirstOrDefaultAsync(r => r.ReservationId == dto.ReservationId);

            if (reservation == null)
                throw new NotFoundException("Reservation not found.");

            if (reservation.Status == ReservationStatus.Cancelled)
                throw new PaymentException("Cannot pay for cancelled reservation.");

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
        // UPDATE STATUS (ADMIN)
        // ============================================
        public async Task<TransactionResponseDto> UpdatePaymentStatusAsync(
            Guid transactionId,
            UpdatePaymentStatusDto dto)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null)
                throw new NotFoundException("Transaction not found.");

            transaction.Status = dto.Status;

            await _context.SaveChangesAsync();

            return MapToDto(transaction);
        }

        // ============================================
        // REFUND LOGIC
        // ============================================
        public async Task<TransactionResponseDto> RefundAsync(
            Guid transactionId,
            RefundRequestDto dto)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Reservation)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null)
                throw new NotFoundException("Transaction not found.");

            if (transaction.Status != PaymentStatus.Success)
                throw new PaymentException("Only successful payments can be refunded.");

            transaction.Status = PaymentStatus.Refunded;

            if (transaction.Reservation != null)
            {
                transaction.Reservation.Status = ReservationStatus.Cancelled;
                transaction.Reservation.CancelledDate = DateTime.UtcNow;
                transaction.Reservation.CancellationReason = dto.Reason;
            }

            await _context.SaveChangesAsync();

            return MapToDto(transaction);
        }

        // ============================================
        // PAGINATED LIST
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
                .Select(t => MapToDto(t))
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
