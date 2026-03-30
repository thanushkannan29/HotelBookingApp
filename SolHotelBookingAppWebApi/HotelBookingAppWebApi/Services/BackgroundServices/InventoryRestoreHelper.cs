using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services.BackgroundServices
{
    /// <summary>
    /// Shared helper for restoring reserved inventory when reservations are cancelled
    /// by background services. Extracted to eliminate duplication across all three services.
    /// </summary>
    internal static class InventoryRestoreHelper
    {
        /// <summary>
        /// Builds a lookup of inventory records keyed by (RoomTypeId, Date)
        /// for all reservations in the batch — single DB query.
        /// </summary>
        internal static async Task<Dictionary<(Guid RoomTypeId, DateOnly Date), RoomTypeInventory>>
            BuildInventoryLookupAsync(
                IEnumerable<Reservation> reservations,
                IRepository<Guid, RoomTypeInventory> inventoryRepo,
                CancellationToken ct)
        {
            var roomTypeIds = reservations
                .SelectMany(r => r.ReservationRooms!)
                .Select(rr => rr.RoomTypeId)
                .Distinct()
                .ToList();

            var allDates = reservations
                .SelectMany(r => GetDateRange(r.CheckInDate, r.CheckOutDate))
                .Distinct()
                .ToList();

            var inventories = await inventoryRepo.GetQueryable()
                .Where(i => roomTypeIds.Contains(i.RoomTypeId) && allDates.Contains(i.Date))
                .ToListAsync(ct);

            return inventories
                .GroupBy(i => (i.RoomTypeId, i.Date))
                .ToDictionary(g => g.Key, g => g.First());
        }

        /// <summary>
        /// Restores reserved inventory for a single reservation using the pre-built lookup.
        /// </summary>
        internal static void RestoreInventory(
            Reservation reservation,
            Dictionary<(Guid RoomTypeId, DateOnly Date), RoomTypeInventory> lookup)
        {
            if (!(reservation.ReservationRooms?.Any() ?? false)) return;

            var roomTypeId = reservation.ReservationRooms.First().RoomTypeId;
            var roomCount = reservation.ReservationRooms.Count;

            foreach (var date in GetDateRange(reservation.CheckInDate, reservation.CheckOutDate))
            {
                if (lookup.TryGetValue((roomTypeId, date), out var inv))
                    inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);
            }
        }

        private static IEnumerable<DateOnly> GetDateRange(DateOnly checkIn, DateOnly checkOut)
        {
            var days = checkOut.DayNumber - checkIn.DayNumber;
            return Enumerable.Range(0, days).Select(d => checkIn.AddDays(d));
        }
    }
}
