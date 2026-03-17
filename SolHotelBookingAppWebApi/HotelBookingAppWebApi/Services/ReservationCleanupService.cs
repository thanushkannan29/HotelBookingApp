using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
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

                var reservationRepo = scope.ServiceProvider
                    .GetRequiredService<IRepository<Guid, Reservation>>();

                var inventoryRepo = scope.ServiceProvider
                    .GetRequiredService<IRepository<Guid, RoomTypeInventory>>();

                var unitOfWork = scope.ServiceProvider
                    .GetRequiredService<IUnitOfWork>();

                try
                {
                    await unitOfWork.BeginTransactionAsync();

                    var now = DateTime.UtcNow;

                    
                    // GET EXPIRED RESERVATIONS
                    
                    var expiredReservations = await reservationRepo.GetQueryable()
                        .Include(r => r.ReservationRooms)
                        .Where(r =>
                            r.Status == ReservationStatus.Pending &&
                            r.ExpiryTime != null &&
                            r.ExpiryTime < now)
                        .ToListAsync(stoppingToken);

                    if (!expiredReservations.Any())
                    {
                        await unitOfWork.RollbackAsync();
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    
                    // PREPARE DATA (OPTIMIZED)
                    

                    var roomTypeIds = expiredReservations
                        .SelectMany(r => r.ReservationRooms)
                        .Select(rr => rr.RoomTypeId)
                        .Distinct()
                        .ToList();

                    var allDates = expiredReservations
                        .SelectMany(r =>
                        {
                            var totalDays = r.CheckOutDate.DayNumber - r.CheckInDate.DayNumber;

                            return Enumerable.Range(0, totalDays)
                                .Select(d => r.CheckInDate.AddDays(d));
                        })
                        .Distinct()
                        .ToList();

                    
                    // FETCH INVENTORY IN ONE QUERY
                    
                    var inventories = await inventoryRepo.GetQueryable()
                        .Where(i =>
                            roomTypeIds.Contains(i.RoomTypeId) &&
                            allDates.Contains(i.Date))
                        .ToListAsync(stoppingToken);

                    // Convert to dictionary for fast lookup
                    var inventoryLookup = inventories
                        .GroupBy(i => new { i.RoomTypeId, i.Date })
                        .ToDictionary(g => g.Key, g => g.First());

                    
                    // PROCESS RESERVATIONS
                    
                    foreach (var reservation in expiredReservations)
                    {
                        if (!reservation.ReservationRooms.Any())
                            continue;

                        var roomTypeId = reservation.ReservationRooms.First().RoomTypeId;
                        var numberOfRooms = reservation.ReservationRooms.Count;

                        var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

                        for (int d = 0; d < totalDays; d++)
                        {
                            var date = reservation.CheckInDate.AddDays(d);

                            var key = new { RoomTypeId = roomTypeId, Date = date };

                            if (inventoryLookup.TryGetValue(key, out var inventory))
                            {
                                inventory.ReservedInventory = Math.Max(
                                    0,
                                    inventory.ReservedInventory - numberOfRooms
                                );
                            }
                        }

                        // Update reservation
                        reservation.Status = ReservationStatus.Cancelled;
                        reservation.CancellationReason = "Payment timeout";
                        reservation.CancelledDate = now;
                    }

                    await unitOfWork.CommitAsync();
                }
                catch
                {
                    await unitOfWork.RollbackAsync();
                    throw;
                }

                
                // RUN EVERY 5 MINUTES
                
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
