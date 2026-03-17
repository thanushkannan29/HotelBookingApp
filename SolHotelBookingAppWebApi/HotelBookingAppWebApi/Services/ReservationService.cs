using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, Room> _roomRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo;
        private readonly IRepository<Guid, RoomTypeRate> _rateRepo;
        private readonly IRepository<Guid, ReservationRoom> _reservationRoomRepo;

        public ReservationService(
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, RoomTypeRate> rateRepo,
            IRepository<Guid, ReservationRoom> reservationRoomRepo)
        {
            _reservationRepo = reservationRepo;
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _inventoryRepo = inventoryRepo;
            _rateRepo = rateRepo;
            _reservationRoomRepo = reservationRoomRepo;
        }

        #region CREATE

        public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (dto.CheckInDate < today)
                throw new ValidationException("Cannot book past dates");

            if (dto.CheckInDate >= dto.CheckOutDate)
                throw new ValidationException("Invalid date range");

            var totalDays = dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;

            var dates = Enumerable.Range(0, totalDays)
                .Select(d => dto.CheckInDate.AddDays(d))
                .ToList();

            //  RoomType
            var roomType = await _roomTypeRepo.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    r.HotelId == dto.HotelId &&
                    r.IsActive);

            if (roomType == null)
                throw new NotFoundException("Invalid hotel or room type");

            //  Physical Rooms
            var physicalRooms = await _roomRepo.GetQueryable()
                .Where(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    r.HotelId == dto.HotelId &&
                    r.IsActive)
                .CountAsync();

            if (dto.NumberOfRooms <= 0)
                throw new ValidationException("Number of rooms must be greater than 0");

            if (dto.NumberOfRooms > physicalRooms)
                throw new InsufficientInventoryException($"Only {physicalRooms} rooms available");

            //  Inventory
            var inventories = await _inventoryRepo.GetQueryable()
                .Where(i =>
                    i.RoomTypeId == dto.RoomTypeId &&
                    dates.Contains(i.Date))
                .ToListAsync();

            if (inventories.Count != dates.Count)
                throw new InsufficientInventoryException("Inventory missing");

            // ✅ Rates
            var rates = await _rateRepo.GetQueryable()
                .Where(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    r.StartDate <= dto.CheckOutDate &&
                    r.EndDate >= dto.CheckInDate)
                .ToListAsync();

            decimal totalAmount = 0;

            foreach (var date in dates)
            {
                var inventory = inventories.First(i => i.Date == date);

                if (inventory.AvailableInventory < dto.NumberOfRooms)
                    throw new InsufficientInventoryException($"Insufficient inventory for {date}");

                var rate = rates.FirstOrDefault(r =>
                    date >= r.StartDate && date <= r.EndDate);

                if (rate == null)
                    throw new RateNotFoundException($"Rate missing for {date}");

                totalAmount += rate.Rate * dto.NumberOfRooms;
            }

            //  Create Reservation
            var reservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                ReservationCode = GenerateReservationCode(),
                UserId = userId,
                HotelId = dto.HotelId,
                CheckInDate = dto.CheckInDate,
                CheckOutDate = dto.CheckOutDate,
                TotalAmount = totalAmount,
                Status = ReservationStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddMinutes(10)
            };

            await _reservationRepo.AddAsync(reservation);

            //  Available Rooms
            var availableRooms = await _roomRepo.GetQueryable()
                .Where(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    r.HotelId == dto.HotelId &&
                    r.IsActive)
                .ToListAsync();

            var selectedRooms = availableRooms.Take(dto.NumberOfRooms).ToList();

            if (selectedRooms.Count < dto.NumberOfRooms)
                throw new InsufficientInventoryException("Not enough rooms available");

            decimal pricePerNight = totalAmount / totalDays / dto.NumberOfRooms;

            foreach (var room in selectedRooms)
            {
                await _reservationRoomRepo.AddAsync(new ReservationRoom
                {
                    ReservationRoomId = Guid.NewGuid(),
                    ReservationId = reservation.ReservationId,
                    RoomTypeId = dto.RoomTypeId,
                    RoomId = room.RoomId,
                    PricePerNight = pricePerNight
                });
            }

            //  Update Inventory
            foreach (var inventory in inventories)
                inventory.ReservedInventory += dto.NumberOfRooms;

            return new ReservationResponseDto
            {
                ReservationId = reservation.ReservationId,
                ReservationCode = reservation.ReservationCode,
                TotalAmount = totalAmount,
                Status = reservation.Status.ToString()
            };
        }

        #endregion

        #region GET

        public async Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string code)
        {
            var reservation = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .FirstOrDefaultAsync(r =>
                    r.ReservationCode == code &&
                    r.UserId == userId);

            if (reservation == null)
                throw new NotFoundException("Reservation not found");

            var rooms = reservation.ReservationRooms!;

            return new ReservationDetailsDto
            {
                ReservationCode = reservation.ReservationCode,
                HotelId = reservation.HotelId,
                RoomTypeId = rooms.First().RoomTypeId,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                NumberOfRooms = rooms.Count,
                TotalAmount = reservation.TotalAmount,
                Status = reservation.Status.ToString()
            };
        }

        public async Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId)
        {
            var reservations = await _reservationRepo.GetQueryable()
                .Include(r => r.ReservationRooms)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return reservations.Select(r => new ReservationDetailsDto
            {
                ReservationCode = r.ReservationCode,
                HotelId = r.HotelId,
                RoomTypeId = r.ReservationRooms!.First().RoomTypeId,
                NumberOfRooms = r.ReservationRooms.Count,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                TotalAmount = r.TotalAmount,
                Status = r.Status.ToString()
            });
        }

        #endregion

        #region CANCEL

        public async Task<bool> CancelReservationAsync(Guid userId, string code, string reason)
        {
            var reservation = await _reservationRepo.GetQueryable()
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

            var totalDays = reservation.CheckOutDate.DayNumber - reservation.CheckInDate.DayNumber;

            var dates = Enumerable.Range(0, totalDays)
                .Select(d => reservation.CheckInDate.AddDays(d))
                .ToList();

            var roomTypeId = reservation.ReservationRooms!.First().RoomTypeId;

            var inventories = await _inventoryRepo.GetQueryable()
                .Where(i =>
                    i.RoomTypeId == roomTypeId &&
                    dates.Contains(i.Date))
                .ToListAsync();

            foreach (var inventory in inventories)
                inventory.ReservedInventory -= reservation.ReservationRooms.Count;

            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledDate = DateTime.UtcNow;
            reservation.CancellationReason = reason;

            return true;
        }

        #endregion

        #region COMPLETE

        public async Task<bool> CompleteReservationAsync(string code)
        {
            var reservation = await _reservationRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.ReservationCode == code);

            if (reservation == null)
                throw new NotFoundException("Reservation not found");

            if (reservation.Status != ReservationStatus.Confirmed)
                throw new ValidationException("Only confirmed reservations can be completed");

            reservation.Status = ReservationStatus.Completed;

            return true;
        }

        #endregion

        private string GenerateReservationCode()
        {
            return "RES-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        }
    }
}
