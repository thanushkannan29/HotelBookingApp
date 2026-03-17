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
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IUnitOfWork unitOfWork)
        {
            _transactionRepo = transactionRepo;
            _reservationRepo = reservationRepo;
            _inventoryRepo = inventoryRepo;
            _unitOfWork = unitOfWork;
        }

        #region CREATE PAYMENT

        public async Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var reservation = await _reservationRepo.GetQueryable()
                    .Include(r => r.Transactions)
                    .FirstOrDefaultAsync(r => r.ReservationId == dto.ReservationId)
                    ?? throw new NotFoundException("Reservation not found");

                if (reservation.Status == ReservationStatus.Cancelled)
                    throw new PaymentException("Cannot pay cancelled reservation");

                if (reservation.Transactions.Any(t => t.Status == PaymentStatus.Success))
                    throw new PaymentException("Already paid");

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

        #endregion

        #region REFUND

        public async Task<TransactionResponseDto> RefundAsync(Guid transactionId, RefundRequestDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var transaction = await _transactionRepo.GetQueryable()
                    .Include(t => t.Reservation)
                    .ThenInclude(r => r.ReservationRooms)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId)
                    ?? throw new NotFoundException("Transaction not found");

                if (transaction.Status != PaymentStatus.Success)
                    throw new PaymentException("Only successful payments can be refunded");

                var reservation = transaction.Reservation;

                if (reservation.Status == ReservationStatus.Completed)
                    throw new PaymentException("Completed reservations cannot be refunded");

                // Update statuses
                transaction.Status = PaymentStatus.Refunded;
                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledDate = DateTime.UtcNow;
                reservation.CancellationReason = dto.Reason;

                // Inventory restore
                var room = reservation.ReservationRooms.First();
                var roomCount = reservation.ReservationRooms.Count;

                var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

                var dates = Enumerable.Range(0, totalDays)
                    .Select(d => reservation.CheckInDate.AddDays(d))
                    .ToList();

                var inventories = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == room.RoomTypeId && dates.Contains(i.Date))
                    .ToListAsync();

                foreach (var inv in inventories)
                {
                    inv.ReservedInventory -= roomCount;
                }

                await _unitOfWork.CommitAsync();

                return MapToDto(transaction);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region GET ALL (PAGINATION)

        public async Task<PagedTransactionResponseDto> GetAllTransactionsAsync(int page, int pageSize)
        {
            var query = _transactionRepo.GetQueryable();

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

        #endregion

        #region HELPER

        private static TransactionResponseDto MapToDto(Transaction t) => new()
        {
            TransactionId = t.TransactionId,
            ReservationId = t.ReservationId,
            Amount = t.Amount,
            PaymentMethod = t.PaymentMethod,
            Status = t.Status,
            TransactionDate = t.TransactionDate
        };

        #endregion
    }
}
