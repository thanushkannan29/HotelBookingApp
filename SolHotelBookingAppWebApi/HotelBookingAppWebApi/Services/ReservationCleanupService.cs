using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;
namespace HotelBookingAppWebApi.Services
{
    public class ReservationCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ReservationCleanupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<HotelBookingContext>();

                var expired = await context.Reservations
                    .Include(r => r.ReservationRooms)
                    .Where(r =>
                        r.Status == ReservationStatus.Pending &&
                        r.ExpiryTime != null &&
                        r.ExpiryTime < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                foreach (var reservation in expired)
                {
                    var room = reservation.ReservationRooms!.FirstOrDefault();

                    if (room == null)
                        continue;

                    var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

                    var dates = Enumerable.Range(0, totalDays)
                        .Select(d => reservation.CheckInDate.AddDays(d))
                        .ToList();

                    var inventories = await context.RoomTypeInventories
                        .Where(i => i.RoomTypeId == room.RoomTypeId && dates.Contains(i.Date))
                        .ToListAsync(stoppingToken);

                    foreach (var inv in inventories)
                    {
                        inv.ReservedInventory =
                            Math.Max(0, inv.ReservedInventory - room.NumberOfRooms);
                    }

                    reservation.Status = ReservationStatus.Cancelled;
                    reservation.CancellationReason = "Payment timeout";
                    reservation.CancelledDate = DateTime.UtcNow;
                }

                await context.SaveChangesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }


}
