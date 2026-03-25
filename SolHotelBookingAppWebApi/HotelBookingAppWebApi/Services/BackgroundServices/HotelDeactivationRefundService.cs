using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    /// <summary>
    /// Runs every 5 minutes. When a hotel becomes inactive, all its Confirmed reservations
    /// are cancelled and their payments are refunded automatically.
    /// </summary>
    public class HotelDeactivationRefundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HotelDeactivationRefundService> _logger;

        public HotelDeactivationRefundService(
            IServiceScopeFactory scopeFactory,
            ILogger<HotelDeactivationRefundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HotelDeactivationRefundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDeactivatedHotelsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in HotelDeactivationRefundService.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessDeactivatedHotelsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
            var transactionRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Transaction>>();
            var inventoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, RoomTypeInventory>>();
            var refundRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, RefundRequest>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Find all Confirmed reservations whose hotel is inactive
            var affectedReservations = await reservationRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Include(r => r.ReservationRooms)
                .Include(r => r.Transactions)
                .Where(r =>
                    r.Status == ReservationStatus.Confirmed &&
                    r.Hotel != null &&
                    !r.Hotel.IsActive)
                .ToListAsync(ct);

            if (!affectedReservations.Any()) return;

            _logger.LogInformation(
                "Hotel deactivation: processing {Count} confirmed reservations for auto-refund.",
                affectedReservations.Count);

            var roomTypeIds = affectedReservations
                .SelectMany(r => r.ReservationRooms!)
                .Select(rr => rr.RoomTypeId).Distinct().ToList();

            var allDates = affectedReservations
                .SelectMany(r => Enumerable.Range(0,
                    r.CheckOutDate.DayNumber - r.CheckInDate.DayNumber)
                    .Select(d => r.CheckInDate.AddDays(d)))
                .Distinct().ToList();

            var inventories = await inventoryRepo.GetQueryable()
                .Where(i => roomTypeIds.Contains(i.RoomTypeId) && allDates.Contains(i.Date))
                .ToListAsync(ct);

            var invLookup = inventories
                .GroupBy(i => new { i.RoomTypeId, i.Date })
                .ToDictionary(g => g.Key, g => g.First());

            var now = DateTime.UtcNow;

            await unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var reservation in affectedReservations)
                {
                    // 1. Cancel the reservation
                    reservation.Status = ReservationStatus.Cancelled;
                    reservation.CancellationReason = "Hotel deactivated — automatic cancellation and refund.";
                    reservation.CancelledDate = now;

                    // 2. Restore inventory
                    if (reservation.ReservationRooms?.Any() ?? false)
                    {
                        var roomTypeId = reservation.ReservationRooms.First().RoomTypeId;
                        var roomCount = reservation.ReservationRooms.Count;
                        var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

                        for (int d = 0; d < totalDays; d++)
                        {
                            var date = reservation.CheckInDate.AddDays(d);
                            var key = new { RoomTypeId = roomTypeId, Date = date };
                            if (invLookup.TryGetValue(key, out var inv))
                                inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);
                        }
                    }

                    // 3. Mark any successful transaction as Refunded
                    var successTx = reservation.Transactions?
                        .FirstOrDefault(t => t.Status == PaymentStatus.Success);

                    if (successTx != null)
                    {
                        successTx.Status = PaymentStatus.Refunded;

                        // 4. Auto-approve a refund request and credit wallet
                        var alreadyExists = await refundRepo.GetQueryable()
                            .AnyAsync(rr => rr.ReservationId == reservation.ReservationId, ct);

                        if (!alreadyExists)
                        {
                            await refundRepo.AddAsync(new RefundRequest
                            {
                                RefundRequestId = Guid.NewGuid(),
                                ReservationId = reservation.ReservationId,
                                UserId = reservation.UserId,
                                Reason = "Hotel deactivated by admin/SuperAdmin.",
                                Status = RefundRequestStatus.Approved,
                                AdminResponse = "Auto-approved due to hotel deactivation.",
                                CreatedAt = now,
                                ProcessedAt = now
                            });

                            // Credit refund amount to guest wallet
                            var walletService = scope.ServiceProvider.GetRequiredService<HotelBookingAppWebApi.Interfaces.IWalletService>();
                            var refundAmount = reservation.FinalAmount > 0 ? reservation.FinalAmount : reservation.TotalAmount;
                            await walletService.CreditAsync(
                                reservation.UserId,
                                refundAmount,
                                $"Refund for cancelled reservation {reservation.ReservationCode} (hotel deactivated)");
                        }
                    }
                }

                await unitOfWork.CommitAsync();
                _logger.LogInformation("Hotel deactivation refund processing committed.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Rollback during HotelDeactivationRefund.");
            }
        }
    }
}
