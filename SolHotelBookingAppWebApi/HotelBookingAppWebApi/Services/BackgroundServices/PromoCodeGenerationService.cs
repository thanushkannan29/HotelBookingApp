using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    public class PromoCodeGenerationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PromoCodeGenerationService> _logger;

        public PromoCodeGenerationService(
            IServiceScopeFactory scopeFactory,
            ILogger<PromoCodeGenerationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var promoService = scope.ServiceProvider.GetRequiredService<IPromoCodeService>();
                    var reservationRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, Reservation>>();
                    var promoRepo = scope.ServiceProvider.GetRequiredService<IRepository<Guid, PromoCode>>();

                    // Find completed reservations without a promo code
                    var existingPromoReservationIds = await promoRepo.GetQueryable()
                        .Select(p => p.ReservationId)
                        .ToListAsync(stoppingToken);

                    var completedReservations = await reservationRepo.GetQueryable()
                        .Where(r => r.Status == ReservationStatus.Completed &&
                                    !existingPromoReservationIds.Contains(r.ReservationId))
                        .ToListAsync(stoppingToken);

                    foreach (var reservation in completedReservations)
                    {
                        try
                        {
                            await promoService.GeneratePromoForCompletedReservationAsync(reservation.ReservationId);
                            _logger.LogInformation("Promo code generated for reservation {Code}", reservation.ReservationCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to generate promo for reservation {Id}", reservation.ReservationId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PromoCodeGenerationService");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
