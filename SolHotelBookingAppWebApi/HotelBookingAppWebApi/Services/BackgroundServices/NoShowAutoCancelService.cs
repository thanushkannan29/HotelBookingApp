using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    /// <summary>
    /// Runs every 5 minutes. Marks confirmed reservations as NoShow when:
    ///   - Today is past the CheckOutDate
    ///   - The guest never checked in (IsCheckedIn == false)
    /// No refund is issued for no-shows.
    /// </summary>
    public class NoShowAutoCancelService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NoShowAutoCancelService> _logger;

        public NoShowAutoCancelService(
            IServiceScopeFactory scopeFactory,
            ILogger<NoShowAutoCancelService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NoShowAutoCancelService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNoShowsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NoShowAutoCancelService.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessNoShowsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
            var inventoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, RoomTypeInventory>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Confirmed reservations that are past checkout and guest never checked in
            var noShows = await reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .Where(r =>
                    r.Status == ReservationStatus.Confirmed &&
                    r.IsCheckedIn == false &&
                    r.CheckOutDate < today)
                .ToListAsync(ct);

            if (!noShows.Any()) return;

            _logger.LogInformation("NoShow processing: {Count} reservations.", noShows.Count);

            var roomTypeIds = noShows
                .SelectMany(r => r.ReservationRooms!)
                .Select(rr => rr.RoomTypeId).Distinct().ToList();

            var allDates = noShows
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
                foreach (var reservation in noShows)
                {
                    reservation.Status = ReservationStatus.NoShow;
                    reservation.CancellationReason = "No-show: guest did not check in before checkout date.";
                    reservation.CancelledDate = now;

                    // Restore inventory (rooms are freed)
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
                }

                await unitOfWork.CommitAsync();
                _logger.LogInformation("NoShow processing committed.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Rollback during NoShowAutoCancel.");
            }
        }
    }
}
