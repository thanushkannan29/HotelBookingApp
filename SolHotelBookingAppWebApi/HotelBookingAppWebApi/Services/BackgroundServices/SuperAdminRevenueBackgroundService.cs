using HotelBookingAppWebApi.Interfaces;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    public class SuperAdminRevenueBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SuperAdminRevenueBackgroundService> _logger;

        public SuperAdminRevenueBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SuperAdminRevenueBackgroundService> logger)
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
                    var service = scope.ServiceProvider.GetRequiredService<ISuperAdminRevenueService>();
                    await service.ProcessCompletedReservationsAsync();
                    _logger.LogInformation("SuperAdmin revenue processing completed at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SuperAdminRevenueBackgroundService");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
