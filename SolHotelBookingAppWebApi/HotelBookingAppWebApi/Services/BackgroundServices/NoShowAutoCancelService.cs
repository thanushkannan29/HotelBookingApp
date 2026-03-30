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
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NoShowAutoCancelService> _logger;

        public NoShowAutoCancelService(
            IServiceScopeFactory scopeFactory,
            ILogger<NoShowAutoCancelService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ── BACKGROUND LOOP ───────────────────────────────────────────────────

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started.", nameof(NoShowAutoCancelService));

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunSafeAsync(stoppingToken);
                await Task.Delay(PollingInterval, stoppingToken);
            }
        }

        // ── PROCESSING ────────────────────────────────────────────────────────

        private async Task RunSafeAsync(CancellationToken ct)
        {
            try
            {
                await ProcessNoShowsAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Service}.", nameof(NoShowAutoCancelService));
            }
        }

        private async Task ProcessNoShowsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
            var inventoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, RoomTypeInventory>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var noShows = await FetchNoShowReservationsAsync(reservationRepo, ct);
            if (!noShows.Any()) return;

            _logger.LogInformation("NoShow processing: {Count} reservations.", noShows.Count);

            var inventoryLookup = await InventoryRestoreHelper
                .BuildInventoryLookupAsync(noShows, inventoryRepo, ct);

            await CommitNoShowsAsync(noShows, inventoryLookup, unitOfWork);
        }

        private static async Task<List<Reservation>> FetchNoShowReservationsAsync(
            IRepository<Guid, Reservation> reservationRepo, CancellationToken ct)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return await reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .Where(r =>
                    r.Status == ReservationStatus.Confirmed &&
                    r.IsCheckedIn == false &&
                    r.CheckOutDate < today)
                .ToListAsync(ct);
        }

        private async Task CommitNoShowsAsync(
            List<Reservation> noShows,
            Dictionary<(Guid RoomTypeId, DateOnly Date), RoomTypeInventory> inventoryLookup,
            IUnitOfWork unitOfWork)
        {
            var now = DateTime.UtcNow;
            await unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var reservation in noShows)
                {
                    MarkAsNoShow(reservation, now);
                    InventoryRestoreHelper.RestoreInventory(reservation, inventoryLookup);
                }

                await unitOfWork.CommitAsync();
                _logger.LogInformation("NoShow processing committed.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Rollback during {Service}.", nameof(NoShowAutoCancelService));
            }
        }

        private static void MarkAsNoShow(Reservation reservation, DateTime now)
        {
            reservation.Status = ReservationStatus.NoShow;
            reservation.CancellationReason = "No-show: guest did not check in before checkout date.";
            reservation.CancelledDate = now;
        }
    }
}
