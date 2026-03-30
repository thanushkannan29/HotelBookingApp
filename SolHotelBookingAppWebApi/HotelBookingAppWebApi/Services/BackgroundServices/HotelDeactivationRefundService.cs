using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    /// <summary>
    /// Runs every 5 minutes. When a hotel becomes inactive, all its Confirmed reservations
    /// are cancelled and their payments are fully refunded to the guest wallet.
    /// </summary>
    public class HotelDeactivationRefundService : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HotelDeactivationRefundService> _logger;

        public HotelDeactivationRefundService(
            IServiceScopeFactory scopeFactory,
            ILogger<HotelDeactivationRefundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ── BACKGROUND LOOP ───────────────────────────────────────────────────

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started.", nameof(HotelDeactivationRefundService));

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
                await ProcessDeactivatedHotelsAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Service}.", nameof(HotelDeactivationRefundService));
            }
        }

        private async Task ProcessDeactivatedHotelsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
            var inventoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, RoomTypeInventory>>();
            var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var affected = await FetchAffectedReservationsAsync(reservationRepo, ct);
            if (!affected.Any()) return;

            _logger.LogInformation(
                "Hotel deactivation: processing {Count} confirmed reservations for auto-refund.",
                affected.Count);

            var inventoryLookup = await InventoryRestoreHelper
                .BuildInventoryLookupAsync(affected, inventoryRepo, ct);

            await CommitCancellationsAsync(affected, inventoryLookup, walletService, unitOfWork);
        }

        private static async Task<List<Reservation>> FetchAffectedReservationsAsync(
            IRepository<Guid, Reservation> reservationRepo, CancellationToken ct)
        {
            return await reservationRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Include(r => r.ReservationRooms)
                .Include(r => r.Transactions)
                .Where(r =>
                    r.Status == ReservationStatus.Confirmed &&
                    r.Hotel != null &&
                    !r.Hotel.IsActive)
                .ToListAsync(ct);
        }

        private async Task CommitCancellationsAsync(
            List<Reservation> reservations,
            Dictionary<(Guid RoomTypeId, DateOnly Date), RoomTypeInventory> inventoryLookup,
            IWalletService walletService,
            IUnitOfWork unitOfWork)
        {
            var now = DateTime.UtcNow;
            await unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var reservation in reservations)
                {
                    CancelReservation(reservation, now);
                    InventoryRestoreHelper.RestoreInventory(reservation, inventoryLookup);
                    MarkSuccessTransactionRefunded(reservation);
                    await IssueWalletRefundAsync(reservation, walletService);
                }

                await unitOfWork.CommitAsync();
                _logger.LogInformation("Hotel deactivation refund processing committed.");
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Rollback during {Service}.", nameof(HotelDeactivationRefundService));
            }
        }

        private static void CancelReservation(Reservation reservation, DateTime now)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancellationReason = "Hotel deactivated — automatic cancellation and full refund.";
            reservation.CancelledDate = now;
        }

        private static void MarkSuccessTransactionRefunded(Reservation reservation)
        {
            var successTx = reservation.Transactions?
                .FirstOrDefault(t => t.Status == PaymentStatus.Success);
            if (successTx is not null)
                successTx.Status = PaymentStatus.Refunded;
        }

        private static async Task IssueWalletRefundAsync(
            Reservation reservation, IWalletService walletService)
        {
            var successTx = reservation.Transactions?
                .FirstOrDefault(t => t.Status == PaymentStatus.Refunded);
            if (successTx is null) return;

            var refundAmount = reservation.FinalAmount > 0
                ? reservation.FinalAmount
                : reservation.TotalAmount;

            await walletService.CreditAsync(
                reservation.UserId,
                refundAmount,
                $"Full refund for cancelled reservation {reservation.ReservationCode} (hotel deactivated)");
        }
    }
}
