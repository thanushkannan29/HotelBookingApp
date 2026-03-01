using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class ReservationService : IReservationService
    {
        private readonly HotelBookingContext _context;

        public ReservationService(HotelBookingContext context)
        {
            _context = context;
        }

        #region CREATE RESERVATION

        public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️ Validate Date Range
                if (dto.CheckInDate >= dto.CheckOutDate)
                    throw new UnableToCreateEntityException("Invalid date range");

                var totalDays = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;

                var dates = Enumerable.Range(0, totalDays)
                    .Select(offset => dto.CheckInDate.AddDays(offset))
                    .ToList();

                // 2️ Validate Hotel + RoomType
                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt =>
                        rt.RoomTypeId == dto.RoomTypeId &&
                        rt.HotelId == dto.HotelId &&
                        rt.IsActive);

                if (roomType == null)
                    throw new NotFoundException("Invalid hotel or room type");

                // 3️ Validate Physical Rooms
                var physicalRooms = await _context.Rooms
                    .CountAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.HotelId == dto.HotelId &&
                        r.IsActive);

                if (dto.NumberOfRooms > physicalRooms)
                    throw new InsufficientInventoryException(
                        $"Only {physicalRooms} physical rooms available");

                // 4️ Get Inventory
                var inventories = await _context.RoomTypeInventories
                    .Where(i =>
                        i.RoomTypeId == dto.RoomTypeId &&
                        dates.Contains(i.Date))
                    .ToListAsync();

                if (inventories.Count != dates.Count)
                    throw new InsufficientInventoryException("Inventory missing for selected dates");

                // 5️ Get Rates (single query)
                var rates = await _context.RoomTypeRates
                    .Where(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        r.StartDate <= dto.CheckOutDate &&
                        r.EndDate >= dto.CheckInDate)
                    .ToListAsync();

                decimal totalAmount = 0;

                foreach (var date in dates)
                {
                    var inventory = inventories.First(i => i.Date == date);

                    if ((inventory.TotalInventory - inventory.ReservedInventory) < dto.NumberOfRooms)
                        throw new InsufficientInventoryException(
                            $"Insufficient inventory for {date:yyyy-MM-dd}");

                    var rate = rates.FirstOrDefault(r =>
                        date >= r.StartDate &&
                        date <= r.EndDate);

                    if (rate == null)
                        throw new RateNotFoundException(
                            $"Rate not configured for {date:yyyy-MM-dd}");

                    totalAmount += rate.Rate * dto.NumberOfRooms;
                }

                // 6️ Create Reservation
                var reservation = new Reservation
                {
                    ReservationId = Guid.NewGuid(),
                    ReservationCode = GenerateReservationCode(),
                    UserId = userId,
                    HotelId = dto.HotelId,
                    CheckInDate = dto.CheckInDate,
                    CheckOutDate = dto.CheckOutDate,
                    TotalAmount = totalAmount,
                    Status = ReservationStatus.Pending,//after transaction payment only confirm
                    CreatedDate = DateTime.UtcNow
                };

                await _context.Reservations.AddAsync(reservation);

                // 7️ Add ReservationRoom
                await _context.ReservationRooms.AddAsync(new ReservationRoom
                {
                    ReservationRoomId = Guid.NewGuid(),
                    ReservationId = reservation.ReservationId,
                    RoomTypeId = dto.RoomTypeId,
                    NumberOfRooms = dto.NumberOfRooms,
                    PricePerNight = totalAmount / totalDays / dto.NumberOfRooms
                });

                // 8️ Update Inventory
                foreach (var inventory in inventories)
                    inventory.ReservedInventory += dto.NumberOfRooms;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ReservationResponseDto
                {
                    ReservationId = reservation.ReservationId,
                    ReservationCode = reservation.ReservationCode,
                    TotalAmount = totalAmount,
                    Status = reservation.Status.ToString(),
                    
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region GET

        public async Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string code)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationRooms)
                .FirstOrDefaultAsync(r =>
                    r.ReservationCode == code &&
                    r.UserId == userId);

            if (reservation == null)
                throw new NotFoundException("Reservation not found");

            var room = reservation.ReservationRooms!.First();

            return new ReservationDetailsDto
            {
                ReservationCode = reservation.ReservationCode,
                HotelId = reservation.HotelId,
                RoomTypeId = room.RoomTypeId,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                NumberOfRooms = room.NumberOfRooms,
                TotalAmount = reservation.TotalAmount,
                Status = reservation.Status.ToString()
            };
        }

        public async Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId)
        {
            return await _context.Reservations
                .Where(r => r.UserId == userId)
                .Select(r => new ReservationDetailsDto
                {
                    ReservationCode = r.ReservationCode,
                    HotelId = r.HotelId,
                    RoomTypeId = r.ReservationRooms
                        .Select(rr => rr.RoomTypeId)
                        .FirstOrDefault(),
                    NumberOfRooms = r.ReservationRooms
                        .Select(rr => rr.NumberOfRooms)
                        .FirstOrDefault(),
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    TotalAmount = r.TotalAmount,
                    Status = r.Status.ToString()
                })
                .ToListAsync();
        }


        #endregion

        #region CANCEL

        public async Task<bool> CancelReservationAsync(Guid userId, string code, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.ReservationRooms)
                    .FirstOrDefaultAsync(r =>
                        r.ReservationCode == code &&
                        r.UserId == userId);

                if (reservation == null)
                    throw new NotFoundException("Reservation not found");

                if (reservation.Status == ReservationStatus.Cancelled)
                    throw new ReservationFailedException("Already cancelled");

                if (reservation.Status == ReservationStatus.Completed)
                    throw new ValidationException("Cannot cancel completed reservation");

                var room = reservation.ReservationRooms!.First();

                var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

                var dates = Enumerable.Range(0, totalDays)
                    .Select(offset => reservation.CheckInDate.AddDays(offset))
                    .ToList();

                var inventories = await _context.RoomTypeInventories
                    .Where(i =>
                        i.RoomTypeId == room.RoomTypeId &&
                        dates.Contains(i.Date))
                    .ToListAsync();

                foreach (var inventory in inventories)
                    inventory.ReservedInventory -= room.NumberOfRooms;

                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledDate = DateTime.UtcNow;
                reservation.CancellationReason = reason;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region COMPLETE (ADMIN)

        public async Task<bool> CompleteReservationAsync(string code)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationCode == code);

            if (reservation == null)
                throw new NotFoundException("Reservation not found");

            if (reservation.Status != ReservationStatus.Confirmed)
                throw new ValidationException("Only confirmed reservations can be completed");

            reservation.Status = ReservationStatus.Completed;

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        private string GenerateReservationCode()
        {
            return "RES-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        }
    }
}
