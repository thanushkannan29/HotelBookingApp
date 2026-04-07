using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    /// <summary>
    /// Runs every 5 minutes. Cancels Pending reservations whose payment window has expired,
    /// restores inventory, and refunds any wallet amount pre-deducted at booking time.
    /// </summary>
    public class ReservationCleanupService : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationCleanupService> _logger;

        public ReservationCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ── BACKGROUND LOOP ───────────────────────────────────────────────────

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started.", nameof(ReservationCleanupService));

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
                await ProcessExpiredReservationsAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Service}.", nameof(ReservationCleanupService));
            }
        }

        private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
            var inventoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, RoomTypeInventory>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var expired = await FetchExpiredReservationsAsync(reservationRepo, ct);
            if (!expired.Any()) return;

            _logger.LogInformation("Cleaning up {Count} expired reservations.", expired.Count);

            var inventoryLookup = await InventoryRestoreHelper
                .BuildInventoryLookupAsync(expired, inventoryRepo, ct);

            await CommitCancellationsAsync(expired, inventoryLookup, unitOfWork);

            var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();
            await RefundWalletDeductionsAsync(expired, walletService);
        }

        private static async Task<List<Reservation>> FetchExpiredReservationsAsync(
            IRepository<Guid, Reservation> reservationRepo, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return await reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .Where(r =>
                    r.Status == ReservationStatus.Pending &&
                    r.ExpiryTime != null &&
                    r.ExpiryTime < now)
                .ToListAsync(ct);
        }

        private async Task CommitCancellationsAsync(
            List<Reservation> expired,
            Dictionary<(Guid RoomTypeId, DateOnly Date), RoomTypeInventory> inventoryLookup,
            IUnitOfWork unitOfWork)
        {
            var now = DateTime.UtcNow;
            await unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var reservation in expired)
                {
                    InventoryRestoreHelper.RestoreInventory(reservation, inventoryLookup);
                    CancelExpiredReservation(reservation, now);
                }

                await unitOfWork.CommitAsync();
                _logger.LogInformation("Expired reservation cleanup committed.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Rollback during {Service}.", nameof(ReservationCleanupService));
            }
        }

        private static void CancelExpiredReservation(Reservation reservation, DateTime now)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancellationReason = "Payment timeout — reservation expired automatically.";
            reservation.CancelledDate = now;
        }

        /// <summary>
        /// Runs AFTER commit so the cancellation is persisted even if a refund fails.
        /// </summary>
        private async Task RefundWalletDeductionsAsync(
            List<Reservation> expired, IWalletService walletService)
        {
            foreach (var reservation in expired.Where(r => r.WalletAmountUsed > 0))
            {
                await TryRefundWalletAsync(reservation, walletService);
            }
        }

        private async Task TryRefundWalletAsync(Reservation reservation, IWalletService walletService)
        {
            try
            {
                await walletService.CreditAsync(
                    reservation.UserId,
                    reservation.WalletAmountUsed,
                    $"Wallet refund — reservation {reservation.ReservationCode} expired without payment.");

                _logger.LogInformation(
                    "Refunded ₹{Amount} to user {UserId} for expired reservation {Code}.",
                    reservation.WalletAmountUsed, reservation.UserId, reservation.ReservationCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to refund wallet for expired reservation {Code}.",
                    reservation.ReservationCode);
            }
        }
    }
}
