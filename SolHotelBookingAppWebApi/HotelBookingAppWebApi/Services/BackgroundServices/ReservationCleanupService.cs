using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    /// <summary>
    /// Runs every 5 minutes. Cancels Pending reservations whose payment window has expired
    /// and restores their inventory.
    /// </summary>
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationCleanupService> _logger;

        public ReservationCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReservationCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredReservationsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ReservationCleanupService.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
            var inventoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, RoomTypeInventory>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTime.UtcNow;

            var expired = await reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .Where(r =>
                    r.Status == ReservationStatus.Pending &&
                    r.ExpiryTime != null &&
                    r.ExpiryTime < now)
                .ToListAsync(ct);

            if (!expired.Any()) return;

            _logger.LogInformation("Cleaning up {Count} expired reservations.", expired.Count);

            var roomTypeIds = expired
                .SelectMany(r => r.ReservationRooms!)
                .Select(rr => rr.RoomTypeId)
                .Distinct().ToList();

            var allDates = expired
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

            await unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var reservation in expired)
                {
                    if (!(reservation.ReservationRooms?.Any() ?? false)) continue;

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

                    reservation.Status = ReservationStatus.Cancelled;
                    reservation.CancellationReason = "Payment timeout — reservation expired automatically.";
                    reservation.CancelledDate = now;
                }

                await unitOfWork.CommitAsync();
                _logger.LogInformation("Expired reservation cleanup committed.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Rollback during ReservationCleanup.");
            }
        }
    }
}
